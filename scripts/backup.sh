#!/bin/bash
# Backs up the Postgres data and uploaded resume PDFs to the laptop over
# Tailscale, at most once every $MIN_INTERVAL_DAYS days, and only when the
# laptop is reachable. Meant to be run on a timer (every few hours); it's a
# no-op most of the time by design — the timer just gives it a chance to
# notice the laptop is online.
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

# --- Postgres ---

dump_name="familyhub-${timestamp}.dump"
tmp_dump_path="/tmp/${dump_name}"

docker exec "$DB_CONTAINER" pg_dump -U "$DB_USER" -d "$DB_NAME" -F c -f "/tmp/${dump_name}"
docker cp "${DB_CONTAINER}:/tmp/${dump_name}" "$tmp_dump_path"
docker exec "$DB_CONTAINER" rm -f "/tmp/${dump_name}"

ssh "$LAPTOP_HOST" "mkdir -p ${LAPTOP_BACKUP_DIR}"
scp "$tmp_dump_path" "${LAPTOP_HOST}:${LAPTOP_BACKUP_DIR}/${dump_name}"
rm -f "$tmp_dump_path"

# Rotate: keep only the newest $KEEP_COPIES on the laptop.
ssh "$LAPTOP_HOST" "cd ${LAPTOP_BACKUP_DIR} && ls -t familyhub-*.dump 2>/dev/null | tail -n +$((KEEP_COPIES + 1)) | xargs -r rm -f --"

echo "Backup ${dump_name} copied to laptop and rotated (keeping ${KEEP_COPIES})."

# --- Resume PDFs ---
# Looked up by name suffix rather than hardcoded, since docker compose
# prefixes volume names with the project/directory name.

resume_volume=$(docker volume ls --filter name=resume-storage --format '{{.Name}}' | head -n1)

if [ -n "$resume_volume" ]; then
    resumes_name="familyhub-resumes-${timestamp}.tar.gz"
    tmp_resumes_path="/tmp/${resumes_name}"

    # Run as the invoking user, not root (the container's default) -- otherwise
    # the archive lands root-owned on the host and the rm -f below fails against
    # /tmp's sticky bit, which (with set -e) would abort the script before the
    # state file gets updated.
    docker run --rm --user "$(id -u):$(id -g)" -v "${resume_volume}:/data:ro" -v /tmp:/backup alpine \
        sh -c "tar czf /backup/${resumes_name} -C /data ."

    scp "$tmp_resumes_path" "${LAPTOP_HOST}:${LAPTOP_BACKUP_DIR}/${resumes_name}"
    rm -f "$tmp_resumes_path"

    # Rotate the resume archives the same way as the DB dumps.
    ssh "$LAPTOP_HOST" "cd ${LAPTOP_BACKUP_DIR} && ls -t familyhub-resumes-*.tar.gz 2>/dev/null | tail -n +$((KEEP_COPIES + 1)) | xargs -r rm -f --"

    echo "Resumes archived as ${resumes_name}, copied to laptop and rotated (keeping ${KEEP_COPIES})."
else
    echo "No resume-storage volume found — skipping resume backup."
fi

date +%s > "$STATE_FILE"
