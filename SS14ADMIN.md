# SS14.Admin Setup for Kritters-Refuge

This is the deployment process for adding SS14.Admin to the host running this repository. It is written to help the host determine which prerequisites already exist and which ones still need to be added.

## Requirements

- SS14.Admin needs PostgreSQL access to the same database the game server uses.
- SS14.Admin needs a public hostname.
- SS14.Admin should be served over HTTPS.
- The deployment can use Docker, or another container/service layout if the host already has one.

Relevant repo config:

- `Content.Shared/CCVar/CCVars.Database.cs` defines the database CVars used by the server:
  - `database.engine`
  - `database.pg_host`
  - `database.pg_port`
  - `database.pg_database`
  - `database.pg_username`
  - `database.pg_password`

## 1. Check the game server database

SS14.Admin depends on the same PostgreSQL database as the game server. If the host is still on SQLite, the game server has to be migrated before SS14.Admin can work.

Example server CVars:

```toml
[database]
engine = "postgres"
pg_host = "127.0.0.1"
pg_port = 5432
pg_database = "ss14"
pg_username = "ss14"
pg_password = "<game-db-password>"
```

If the server is already on PostgreSQL, keep the existing database and move on.

If it is still on SQLite, the migration path is:

1. Install PostgreSQL on the host or a reachable database server.
2. Create the game database and a dedicated game-server user.
3. Copy the existing server data into PostgreSQL if the host already has live player/admin data.
4. Update the server config to use `database.engine = "postgres"`.
5. Restart the game server and verify it starts cleanly.

If the host is starting fresh and has no live data yet, create the PostgreSQL database first and point the server at it before launch.

## 2. Create a database user for SS14.Admin

Use a separate PostgreSQL user for the admin site, pointing at the same SS14 database as the game server.

```sql
CREATE USER ss14_admin WITH PASSWORD '<admin-db-password>';
GRANT CONNECT ON DATABASE ss14 TO ss14_admin;
GRANT USAGE ON SCHEMA public TO ss14_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ss14_admin;
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO ss14_admin;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ss14_admin;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO ss14_admin;
```

Adjust the database name, schema, and permissions to match the host’s PostgreSQL layout.

## 3. Check HTTPS

SS14.Admin should not be exposed publicly without TLS.

If the host already has HTTPS, reuse that setup and point the admin site at it.

If the host does not already have HTTPS, put the site behind a reverse proxy and issue a certificate before enabling the admin panel.

### Caddy

```caddy
admin.example.com {
    reverse_proxy 127.0.0.1:27689
}
```

Caddy will handle certificate issuance automatically if the domain points at the host and ports 80/443 are open.

### Nginx

If the host uses Nginx, configure TLS with your existing certificate workflow, then proxy to the local SS14.Admin port:

```nginx
server {
    listen 443 ssl http2;
    server_name admin.example.com;

    ssl_certificate     /etc/letsencrypt/live/admin.example.com/fullchain.pem;
    ssl_certificate_key  /etc/letsencrypt/live/admin.example.com/privkey.pem;

    location / {
        proxy_pass          http://127.0.0.1:27689;
        proxy_http_version  1.1;
        proxy_set_header    Upgrade $http_upgrade;
        proxy_set_header    Connection keep-alive;
        proxy_set_header    Host $host;
        proxy_set_header    X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header    X-Forwarded-Proto https;
        proxy_cache_bypass  $http_upgrade;
    }
}
```

## 4. Check the deployment method

SS14.Admin can be deployed with Docker if the host already uses it for services.

If the host does not use Docker, the same appsettings and reverse-proxy requirements still apply, but the container example should be replaced with the host’s normal service manager or packaging workflow.

## 5. Deploy SS14.Admin

The official image is `ghcr.io/space-wizards/ss14.admin:1`.

If using Docker, create a deployment directory on the host, for example `/opt/ss14_admin/`, and add:

### `docker-compose.yml`

```yaml
services:
  ss14_admin:
    image: ghcr.io/space-wizards/ss14.admin:1
    container_name: ss14_admin
    user: 1654:1654
    volumes:
      - ./appsettings.yml:/app/appsettings.yml:ro
    ports:
      - "127.0.0.1:27689:8080"
    restart: unless-stopped
```

### `appsettings.yml`

```yaml
ConnectionStrings:
  DefaultConnection: "Server=127.0.0.1;Port=5432;Database=ss14;User Id=ss14_admin;Password=<admin-db-password>"

AllowedHosts: "admin.example.com"
urls: "http://0.0.0.0:8080/"
WebRootPath: "/app/wwwroot"

ForwardProxies:
  - 127.0.0.1

Auth:
  Authority: "https://account.spacestation14.com/"
  ClientId: "<oauth-client-id>"
  ClientSecret: "<oauth-client-secret>"

authServer: "https://auth.spacestation14.com"
```

Notes:

- `AllowedHosts` must match the public admin hostname.
- `ForwardProxies` must include the reverse proxy source address if it is not localhost.
- The site should be exposed through HTTPS, not directly over plain HTTP.
- Do not open the container port to the public internet; only the proxy should reach it.

## 6. Register OAuth

Admins log in to SS14.Admin using SS14 account OAuth.

1. Go to `https://account.spacestation14.com/Identity/Account/Manage/Developer`.
2. Create a new OAuth app.
3. Set the authorization callback URL to the admin host plus `/signin-oidc`.
   - Example: `https://admin.example.com/signin-oidc`
4. Set the homepage URL to the public admin URL.
   - Example: `https://admin.example.com`
5. Copy the Client ID into `ClientId`.
6. Generate a new secret and copy it into `ClientSecret`.

The docs note that the secret is only shown once.

The proxy must preserve the forwarded host and protocol headers so OAuth redirect URLs resolve correctly.

## 7. Start and verify

```bash
cd /opt/ss14_admin
docker compose up -d
docker compose logs -f ss14_admin
```

Then verify:

- The admin site loads at the public HTTPS URL.
- OAuth login redirects back to `/signin-oidc` successfully.
- The admin panels can read the same ban/admin data as the game server.
- The game server is using PostgreSQL if SS14.Admin is meant to read the live admin database.

## Troubleshooting

- If the host is still on SQLite, stop here and finish the database migration first.
- If you are not sure whether the host needs PostgreSQL, check the live server config first; SS14.Admin cannot use SQLite.
- If HTTPS is not working yet, finish TLS setup before trying to debug OAuth.
- If you are not sure whether Docker is required, check the host’s existing service layout; Docker is a deployment option, not a requirement.
- If login fails with a generic OAuth error, check the callback URL first.
- If the redirect URI looks wrong, verify the reverse proxy headers and `ForwardProxies`.
- If the site works over HTTP but not HTTPS, fix TLS first; the docs call out cookie/security issues without HTTPS.

## Source

- Official docs: https://docs.spacestation14.com/en/server-hosting/setting-up-ss14-admin.html
