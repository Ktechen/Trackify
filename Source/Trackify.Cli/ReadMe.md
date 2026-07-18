# Trackify CLI

Controls LEGO Powered Up hubs over **onboard Bluetooth (BlueZ)** on a Linux box — meant for a
Raspberry Pi / Linux server. Shares Domain/Application/Infrastructure (and the `trackify.db` store)
with the Trackify app.

**On Windows (dev/test):** the Windows build of the CLI (`net10.0-windows…` TFM — e.g. via the Rider
run configs or `dotnet run -f net10.0-windows10.0.19041.0`) uses **WinRT Bluetooth**, so
`discover`/`drive` work on the dev box too. The plain `net10.0` build has no Windows transport and
reports "Bluetooth is not available".

## Commands

```bash
trackify                       # dashboard: banner, saved trains, command overview
trackify list                  # list saved trains
trackify discover              # scan for hubs (turn the hub on!), --timeout 15
trackify connect "Blauer Zug"  # reachability test (connect + disconnect)
trackify drive   "Blauer Zug" --speed 40 --color Green   # run until Ctrl+C
trackify stop    "Blauer Zug"  # stop the motor
trackify color   "Blauer Zug" Blue                        # set the hub LED
trackify --help                # full help
```

A train is addressed by **name or id** (see `trackify list`).

## The train store (`trackify.db`)

Trains live in a **SQLite** database managed by **EF Core** (`EfTrainStore`). Default path:
`~/.config/Trackify/trackify.db` (Linux) / `%APPDATA%\Trackify\trackify.db` (Windows), overridable
with the `TRACKIFY_STORE` environment variable. The schema is created automatically on first run;
enums are stored as readable names. Same schema as the app — copy the `.db` to the Pi, or point both
at the same file. The hub MAC (`HubId`/`BleAddress`) comes from `trackify discover`.

## Deploy to a Raspberry Pi

```bash
# From Windows for the Pi (arm64), self-contained (no .NET needed on the Pi):
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj -c Release -r linux-arm64 \
  --self-contained -p:TrackifyLinux=true -o publish/
scp -r publish/ pi@raspberrypi:/opt/trackify/
ssh pi@raspberrypi 'chmod +x /opt/trackify/trackify'
```

`-p:TrackifyLinux=true` is required when cross-publishing **from Windows** (the LINUX compile flag is
set from the build host; without it the no-op fallback is compiled instead of BlueZ). Building on the
Pi (or in Docker/CI on Linux) turns it on automatically. Prerequisites on the Pi: `bluetoothd`
running, user in the `bluetooth` group; run `trackify discover` once so BlueZ knows the device.
The CI `cli-arm64.yml` workflow produces this artifact.

## Docker

From the repo root:

```bash
docker compose up -d                       # build + run permanently (auto-restart after reboot)
docker compose logs -f                     # live Serilog output
docker compose run --rm trackify discover  # one-shot commands
docker compose down                        # stops the train cleanly (SIGINT)
```

`docker-compose.yml` uses host networking + a `/var/run/dbus` mount (BLE via the host's `bluetoothd`),
`stop_signal: SIGINT` for a clean stop, and `restart: unless-stopped`. Building inside the Linux
container turns the LINUX flag on automatically, so real BlueZ is compiled in.

## Run permanently at boot (systemd)

`trackify drive` already runs until stopped (Ctrl+C → motor stop + clean disconnect). For autostart:

```ini
# /etc/systemd/system/trackify.service
[Unit]
Description=Trackify LEGO train control
After=bluetooth.target
Requires=bluetooth.service

[Service]
ExecStart=/opt/trackify/trackify drive "Blauer Zug" --speed 40
Restart=on-failure
RestartSec=5
User=pi
Environment=TRACKIFY_STORE=/home/pi/.config/Trackify/trackify.db
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now trackify   # starts now AND on every boot
journalctl -u trackify -f              # live logs
sudo systemctl stop trackify           # stops the train cleanly (SIGINT)
```

---

# Trackify CLI (Deutsch)

Steuert LEGO Powered Up Hubs über das **Onboard-Bluetooth (BlueZ)** eines Linux-Rechners — gedacht
für Raspberry Pi / Linux-Server. Teilt sich Domain/Application/Infrastructure (und den `trackify.db`-
Store) mit der Trackify-App.

**Auf Windows (Entwickeln/Testen):** Der Windows-Build der CLI (`net10.0-windows…`-TFM, z. B. via
Rider-Run-Config oder `dotnet run -f net10.0-windows10.0.19041.0`) nutzt **WinRT-Bluetooth** —
`discover`/`drive` funktionieren also auch am Dev-Rechner. Der plain `net10.0`-Build hat bewusst
keinen Windows-Transport und meldet „Bluetooth is not available".

## Befehle

```bash
trackify                       # Dashboard: Banner, gespeicherte Züge, Befehlsübersicht
trackify list                  # gespeicherte Züge anzeigen
trackify discover              # nach Hubs scannen (Hub einschalten!), --timeout 15
trackify connect "Blauer Zug"  # Erreichbarkeits-Test (verbinden + trennen)
trackify drive   "Blauer Zug" --speed 40 --color Green   # fahren bis Ctrl+C
trackify stop    "Blauer Zug"  # Motor stoppen
trackify color   "Blauer Zug" Blue                        # Hub-LED setzen
trackify --help                # vollständige Hilfe
```

Ein Zug wird per **Name oder Id** angesprochen (siehe `trackify list`).

## Der Train-Store (`trackify.db`)

Züge liegen in einer **SQLite**-Datenbank, verwaltet von **EF Core** (`EfTrainStore`). Standardpfad:
`~/.config/Trackify/trackify.db` (Linux) / `%APPDATA%\Trackify\trackify.db` (Windows), überschreibbar
per Umgebungsvariable `TRACKIFY_STORE`. Das Schema wird beim ersten Start automatisch angelegt; Enums
werden als lesbare Namen gespeichert. Gleiches Schema wie die App — die `.db` auf den Pi kopieren oder
beide auf dieselbe Datei zeigen lassen. Die Hub-MAC (`HubId`/`BleAddress`) liefert `trackify discover`.

## Auf einen Raspberry Pi deployen

```bash
# Von Windows aus für den Pi (arm64), self-contained (kein .NET auf dem Pi nötig):
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj -c Release -r linux-arm64 \
  --self-contained -p:TrackifyLinux=true -o publish/
scp -r publish/ pi@raspberrypi:/opt/trackify/
ssh pi@raspberrypi 'chmod +x /opt/trackify/trackify'
```

`-p:TrackifyLinux=true` ist beim Cross-Publish **von Windows** Pflicht (das LINUX-Flag kommt vom
Build-Host; ohne es wird der No-op-Fallback statt BlueZ kompiliert). Auf dem Pi (bzw. in Docker/CI
unter Linux) ist es automatisch an. Voraussetzungen auf dem Pi: `bluetoothd` läuft, Benutzer in der
`bluetooth`-Gruppe; einmal `trackify discover` ausführen, damit BlueZ das Gerät kennt. Der
CI-Workflow `cli-arm64.yml` erzeugt dieses Artefakt.

## Docker

Aus dem Repo-Root:

```bash
docker compose up -d                       # bauen + dauerhaft starten (Autostart nach Reboot)
docker compose logs -f                     # Serilog-Ausgabe live
docker compose run --rm trackify discover  # Einmal-Befehle
docker compose down                        # stoppt den Zug sauber (SIGINT)
```

`docker-compose.yml` nutzt Host-Networking + `/var/run/dbus`-Mount (BLE über den `bluetoothd` des
Hosts), `stop_signal: SIGINT` für sauberes Stoppen und `restart: unless-stopped`. Da im Linux-
Container gebaut wird, ist das LINUX-Flag automatisch an — echtes BlueZ ist einkompiliert.

## Dauerbetrieb beim Booten (systemd)

`trackify drive` läuft bereits bis zum Stopp (Ctrl+C → Motor-Stopp + sauberes Trennen). Für Autostart
die systemd-Unit oben (englischer Teil) verwenden:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now trackify   # startet jetzt UND bei jedem Boot
journalctl -u trackify -f              # Logs live
sudo systemctl stop trackify           # stoppt den Zug sauber (SIGINT)
```
