# Migrating the host role: laptop → PC

Context: the laptop now travels; the PC is always home. The PC becomes the
permanent server (Docker, Postgres, the GitHub Actions runner, the
port-forward target). The laptop becomes a pure dev/client machine — no
server role at all once this is done.

Do this as one sitting, with both machines reachable (laptop stays up until
the PC is verified working).

## 1. Prep the PC

- [ ] Install Docker Engine + Compose (rootless, matching the laptop setup — no sudo needed for `docker` afterward)
- [ ] Install Tailscale, connect it to the same tailnet
- [ ] Install .NET 10 SDK (the CI workflow runs `dotnet restore/build/test` directly on the runner, not just in Docker — needs the SDK on the host)
- [ ] Set up SSH key-only auth on the PC for your own remote admin access (same pattern as the laptop: your public key in `authorized_keys`, password auth disabled) — this is unrelated to CI, just carries over the "secure remote access" story to the new host
- [ ] `git clone https://github.com/AlbinKlintman/family-hub.git` into wherever the deploy directory should live (e.g. `~/projects/family-hub/WebApp` to mirror the laptop's layout, but doesn't have to match)

## 2. Migrate the data

On the laptop:
```bash
docker exec webapp-db-1 pg_dump -U familyhub -d familyhub -F c -f /tmp/familyhub.dump
docker cp webapp-db-1:/tmp/familyhub.dump ~/familyhub.dump
scp ~/familyhub.dump <pc-tailscale-ip>:~/familyhub.dump   # or use the PC's Tailscale MagicDNS name
```

On the PC (after `docker compose up -d db` has created a fresh empty database there):
```bash
docker cp ~/familyhub.dump webapp-db-1:/tmp/familyhub.dump
docker exec webapp-db-1 pg_restore -U familyhub -d familyhub --clean --if-exists /tmp/familyhub.dump
```
Verify row counts match before trusting it (`SELECT count(*) FROM "AspNetUsers";` etc. on both sides).

## 3. Recreate secrets on the PC

- [ ] `.env` (gitignored, never in git) — new `POSTGRES_PASSWORD`, doesn't need to match the laptop's old one
- [ ] `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` if you ever want to `dotnet run` locally on the PC outside Docker

## 4. Update the CI workflow

Edit `.github/workflows/ci-cd.yml`:
- [ ] `DEPLOY_DIR` env var → the PC's actual clone path (almost certainly a different path/username than the laptop's `/home/albin/projects/family-hub/WebApp`)
- [ ] `runs-on: [self-hosted, homelab]` labels can stay as-is *if* the new runner on the PC is registered with the same labels — no other workflow changes needed, which is exactly why we labeled it generically rather than by hostname

## 5. Move the runner

**On the laptop first** (before starting the PC's runner, so no window exists where two runners could both claim a deploy job and race):
- [ ] `systemctl --user stop github-runner.service`
- [ ] `systemctl --user disable github-runner.service`
- [ ] Deregister it from GitHub: `~/actions-runner/config.sh remove --token <token from gh api -X POST repos/AlbinKlintman/family-hub/actions/runners/registration-token>`

**On the PC**, repeat what we did on the laptop:
- [ ] Download the runner tarball, extract to `~/actions-runner`
- [ ] Get a fresh registration token, `./config.sh --url https://github.com/AlbinKlintman/family-hub --token <token> --name pc-server --labels self-hosted,homelab --unattended`
- [ ] Create the same user-level systemd unit (`~/.config/systemd/user/github-runner.service` — copy the laptop's, it's not host-specific)
- [ ] `loginctl enable-linger <pc-username>`
- [ ] `systemctl --user daemon-reload && systemctl --user enable --now github-runner.service`
- [ ] Confirm online: `gh api repos/AlbinKlintman/family-hub/actions/runners --jq '.runners[] | {name, status}'`

## 6. Cut over and verify

- [ ] Push a trivial commit (or re-run the last workflow) and confirm it runs on the PC's runner and deploys successfully there
- [ ] `docker compose ps` on the PC shows both containers healthy
- [ ] Log in as an existing user on the PC's deployment, confirm the restored data (companies/applications) is there
- [ ] Test phone access via the PC's Tailscale IP
- [ ] Only once all of the above is confirmed: `docker compose down` on the laptop to stop serving stale/duplicate data there

## 7. Still ahead after this (not part of migration itself)

- Caddy reverse proxy + port-forward now targets the PC's LAN IP, not the laptop's
- `albinklintman.com` DNS points at home's public IP either way (host-agnostic, just needs the router's forward rule aimed at the PC)
