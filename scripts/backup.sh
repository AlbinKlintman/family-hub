#!/bin/bash
# Backs up the Postgres data to the laptop over Tailscale, at most once
# every $MIN_INTERVAL_DAYS days, and only when the laptop is reachable.
# Meant to be run on a timer (every few hours); it's a no-op most of the
# time by design — the timer just gives it a chance to notice the laptop
# is online.
set -euo pipefail

DB_CONTAINER="family-hub-db-1"
DB_USER="familyhub"
DB_NAME="familyhub"

LAPTOP_HOST="albin@100.106.208.36"
LAPTOP_BACKUP_DIR="family-hub-backups"
KEEP_COPIES=3

MIN_INTERVAL_DAYS=3
STATE_DIR="$HOME/.family-hub-backup"
STATE_FILE="$STATE_DIR/last-success"

mkdir -p "$STATE_DIR"

if [ -f "$STATE_FILE" ]; then
    last_success=$(cat "$STATE_FILE")
    now=$(date +%s)
    elapsed_days=$(( (now - last_success) / 86400 ))
    if [ "$elapsed_days" -lt "$MIN_INTERVAL_DAYS" ]; then
        echo "Last backup was ${elapsed_days}d ago (< ${MIN_INTERVAL_DAYS}d) — skipping."
        exit 0
    fi
fi

if ! ssh -o ConnectTimeout=5 -o BatchMode=yes "$LAPTOP_HOST" true 2>/dev/null; then
    echo "Laptop unreachable — will retry next tick."
    exit 0
fi

echo "Laptop is online and a backup is due — starting."

timestamp=$(date +%Y%m%d-%H%M%S)
dump_name="familyhub-${timestamp}.dump"
tmp_path="/tmp/${dump_name}"

docker exec "$DB_CONTAINER" pg_dump -U "$DB_USER" -d "$DB_NAME" -F c -f "/tmp/${dump_name}"
docker cp "${DB_CONTAINER}:/tmp/${dump_name}" "$tmp_path"
docker exec "$DB_CONTAINER" rm -f "/tmp/${dump_name}"

ssh "$LAPTOP_HOST" "mkdir -p ${LAPTOP_BACKUP_DIR}"
scp "$tmp_path" "${LAPTOP_HOST}:${LAPTOP_BACKUP_DIR}/${dump_name}"
rm -f "$tmp_path"

# Rotate: keep only the newest $KEEP_COPIES on the laptop.
ssh "$LAPTOP_HOST" "cd ${LAPTOP_BACKUP_DIR} && ls -t familyhub-*.dump 2>/dev/null | tail -n +$((KEEP_COPIES + 1)) | xargs -r rm -f --"

date +%s > "$STATE_FILE"
echo "Backup ${dump_name} copied to laptop and rotated (keeping ${KEEP_COPIES})."
