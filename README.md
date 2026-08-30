# Family Hub

A self-hosted personal web app for tracking the small stuff that matters day
to day — job applications, notes, a shared calendar, and training — with
each account seeing only its own data. Runs on a home server, reachable from
anywhere over Tailscale, with a full CI/CD pipeline deploying every push.

## Features

- **Job Applications** — a Trello-style drag-and-drop kanban board across
  seven stages (Searching → Applied → Test Scheduled → Test Done → Interview
  Scheduled → Interview Done → Rejected), with companies you can attach to
  an application, a chance rating, and an applied date that sets itself
  automatically the first time an application reaches "Applied."
- **Notes** — a combined to-do and laundry-scheduling note list.
- **Calendar** — a shared events calendar with a month view on the home page.
- **Training** — workout logging, weight tracking, and exercise history.
- **Statistics** — charts over the training data.

Every account is fully isolated — no user can see or reach another user's
data, enforced on every query and tested explicitly (cross-user id access
returns 404, a forged cross-user API request is rejected).

## Stack

- ASP.NET Core (Razor Pages) on .NET 10, with ASP.NET Core Identity for auth
- EF Core + PostgreSQL (Npgsql)
- Bootstrap 5 + vanilla JS (SortableJS for the kanban drag-and-drop) — no
  frontend framework, everything vendored locally rather than pulled from a
  CDN
- Docker + Docker Compose
- MailKit for transactional email (account confirmation) over SMTP

## Architecture

- **Self-hosted**, on an always-on home PC, reachable remotely only over
  [Tailscale](https://tailscale.com/) — no public ports are opened on the
  home network. SSH access is key-only, password auth disabled.
- **CI/CD**: GitHub Actions, with the runner installed *on the same server*
  (as a persistent user-level `systemd` service) rather than a GitHub-hosted
  runner reaching in over SSH. A push to `main` runs the test suite, builds
  and pushes a versioned image to GitHub Container Registry, then the deploy
  step is just a local `docker compose pull && up -d` — no deploy keys ever
  leave the machine. The workflow only triggers on `push`, deliberately not
  `pull_request`, since this is a public repo with a self-hosted runner.
- **Backups**: Postgres is dumped and copied to a second machine over
  Tailscale on a schedule (`scripts/backup.sh` + a systemd timer checking
  every few hours), but only actually runs when enough time has passed
  *and* the second machine happens to be online — otherwise it's a no-op
  and waits for the next check. Keeps the 3 most recent copies.

## Local development

```bash
# Postgres connection string (kept out of git — see appsettings.json for the shape)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=familyhub;Username=familyhub;Password=..."
dotnet user-secrets set "Email:Password" "..."

docker compose up -d db   # Postgres only, published on 127.0.0.1:5432
dotnet run
```

Or run the whole stack containerized (copy `.env.example` to `.env` first
and fill in real values):

```bash
docker compose up -d --build
```

Tests: `dotnet test WebApp.slnx`

## Repo layout

- `Pages/` — Razor Pages, one folder per feature area
- `Models/` — EF Core entities
- `WebApp.Tests/` — xUnit test project
- `scripts/` — operational scripts (backup) and their systemd unit files
- `docs/` — runbooks (e.g. migrating the server role to a new machine)
