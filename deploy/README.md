# Deploying HomeRadar to a VPS

Minimal setup: no domain, no HTTPS, no reverse proxy — Kestrel serves directly
on port 8080. You'll reach the app at `http://<your-vps-ip>:8080`.

## 1. Create the VPS

Any provider works; recommended cheap options:
- **DigitalOcean**: $6/mo droplet, 1 GB RAM, Ubuntu 24.04 LTS
- **Hetzner**: ~€4.5/mo CX22, 2 GB RAM, Ubuntu 24.04 LTS (more headroom, cheaper)

When creating it, add your SSH key so you can log in without a password.
Note the server's public IP address once it's up.

## 2. Initial server setup

SSH in as root:

```
ssh root@<your-vps-ip>
```

Update the system and create a dedicated non-root user to run the app:

```bash
apt update && apt upgrade -y
adduser --disabled-password --gecos "" homeradar
mkdir -p /var/www/homeradar
chown homeradar:homeradar /var/www/homeradar
```

Install the ASP.NET Core 9 runtime (just the runtime, not the full SDK —
smaller footprint since you'll publish locally and copy the build over):

```bash
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt-get update
apt-get install -y aspnetcore-runtime-9.0
```

Open the firewall for SSH and the app's port:

```bash
ufw allow OpenSSH
ufw allow 8080/tcp
ufw --force enable
```

## 3. Publish and copy the app (run these locally, on your Windows machine)

From the project folder (`HomeRadar/HomeRadar`):

```
dotnet publish -c Release -o ./publish
scp -r ./publish/* root@<your-vps-ip>:/var/www/homeradar/
```

(`scp` ships with Windows 10/11's built-in OpenSSH client — should work from
PowerShell or Git Bash with no extra install.)

Then fix ownership on the server:

```bash
chown -R homeradar:homeradar /var/www/homeradar
```

The app auto-applies EF Core migrations on startup (see `Program.cs`), so the
SQLite database file (`homeradar.db`) will be created automatically inside
`/var/www/homeradar` the first time it runs — no manual migration step needed
on the server.

## 4. Install the systemd service

Copy `deploy/homeradar.service` (in this repo) to the server:

```
scp deploy/homeradar.service root@<your-vps-ip>:/etc/systemd/system/homeradar.service
```

Then on the server:

```bash
systemctl daemon-reload
systemctl enable homeradar
systemctl start homeradar
systemctl status homeradar
```

`enable` makes it start automatically on reboot; `Restart=always` in the unit
file means it comes back up if it crashes.

## 5. Verify

From your own machine:

```
curl http://<your-vps-ip>:8080/
```

Or just open `http://<your-vps-ip>:8080` in a browser.

## 6. Redeploying after code changes

Each time you change code:

```
dotnet publish -c Release -o ./publish
scp -r ./publish/* root@<your-vps-ip>:/var/www/homeradar/
ssh root@<your-vps-ip> "chown -R homeradar:homeradar /var/www/homeradar && systemctl restart homeradar"
```

Any new EF Core migrations apply automatically on the restart.

## Useful commands on the server

```bash
systemctl status homeradar       # is it running?
journalctl -u homeradar -f       # live logs (Ctrl+C to stop)
journalctl -u homeradar -n 100   # last 100 log lines
systemctl restart homeradar      # restart the service
```

## Notes

- **No HTTPS**: fine for personal use, but login cookies and any traffic are
  sent in plaintext over the network. Don't reuse a password here that
  matters elsewhere. Adding a domain + HTTPS later (via Caddy or similar) is
  a solvable follow-up if you want it.
- **Secrets**: `Telegram:BotToken` is stored via `dotnet user-secrets`
  locally, which does **not** get published or copied to the server — you'll
  need to set it separately on the server, either by adding it to
  `appsettings.Production.json` on the server (not committed to git) or as
  an `Environment=Telegram__BotToken=...` line in the systemd unit file.
- **`SsGe:ClientSecret`** is currently committed in plaintext in
  `appsettings.json` (a deliberate choice made earlier) — it'll be included
  automatically since it's already in the published output.
