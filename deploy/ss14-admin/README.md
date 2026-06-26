# SS14.Admin Deployment

This directory contains deployable templates for running SS14.Admin next to the Kritters-Refuge server.

SS14.Admin must point at the same PostgreSQL database used by the game server. It cannot read live data from SQLite.

## Files

- `docker-compose.yml`: runs the official `ghcr.io/space-wizards/ss14.admin:1` image on localhost port `27689`.
- `appsettings.example.yml`: copy to `appsettings.yml` and fill in the database, hostname, and OAuth values.
- `db-grants.sql`: PostgreSQL grants for a dedicated `ss14_admin` database user.
- `Caddyfile.example`: minimal HTTPS reverse proxy example.
- `nginx.example.conf`: HTTPS reverse proxy example for Nginx.

`appsettings.yml` and `.env` are intentionally gitignored because they contain secrets.

## Setup

1. Verify the game server is using PostgreSQL:

   ```toml
   [database]
   engine = "postgres"
   pg_host = "127.0.0.1"
   pg_port = 5432
   pg_database = "ss14"
   pg_username = "ss14"
   pg_password = "<game-db-password>"
   ```

2. Create the admin database user:

   ```bash
   psql -d ss14 -f db-grants.sql
   ```

   Before running it, replace `<admin-db-password>` and adjust `ss14`/`public` if the live database uses different names.

3. Create the local app settings file:

   ```bash
   cp appsettings.example.yml appsettings.yml
   ```

   Fill in:

   - `ConnectionStrings.DefaultConnection`
   - `AllowedHosts`
   - `Auth.ClientId`
   - `Auth.ClientSecret`

4. Register an OAuth application at:

   ```text
   https://account.spacestation14.com/Identity/Account/Manage/Developer
   ```

   Set the callback URL to:

   ```text
   https://admin.example.com/signin-oidc
   ```

5. Put the service behind HTTPS using Caddy, Nginx, or the host's existing reverse proxy.

6. Start the service:

   ```bash
   docker compose up -d
   docker compose logs -f ss14_admin
   ```

## Checks

- The public admin URL loads over HTTPS.
- OAuth returns to `/signin-oidc` without a redirect URI error.
- Admin panels show the same bans, notes, and admin data as the game server.
- The container port is only bound to `127.0.0.1`; public traffic should go through the HTTPS proxy.

Official docs: https://docs.spacestation14.com/en/server-hosting/setting-up-ss14-admin.html
