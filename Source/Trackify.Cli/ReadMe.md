# Trackify CLI

Controls LEGO Powered Up hubs over **onboard Bluetooth (BlueZ)** on a Linux box — meant for a
Raspberry Pi / Linux server. Shares Domain/Application/Infrastructure (and the `trackify.db` store)
with the Trackify app.

**On Windows (dev/test):** build/run the `net10.0-windows…` TFM (e.g. `dotnet run -f
net10.0-windows10.0.19041.0` or the Rider run config) — it uses **WinRT Bluetooth**, so
`discover`/`drive` work on the dev box too. The plain `net10.0` build (what ships to the Pi) uses
BlueZ on Linux and reports "Bluetooth is not available" off-Linux.

## Commands

```bash
trackify                       # dashboard: banner, saved trains, command overview
trackify list                  # list saved trains
trackify discover              # scan for hubs (turn the hub on!), --timeout 15
trackify connect "Blauer Zug"  # reachability test (connect + disconnect)
trackify drive   "Blauer Zug" --speed 40 --color Green   # run until Ctrl+C
trackify stop    "Blauer Zug"  # stop the motor
trackify color   "Blauer Zug" Blue                        # set the hub LED
trackify auto                  # auto-pilot: run every saved train, re-scan on an interval
trackify --help                # full help
```

A train is addressed by **name or id** (see `trackify list`).

## Auto mode (auto-pilot)

`trackify auto` is a long-running loop meant for **unattended operation** on the Pi (systemd / Docker).
Every `--interval` seconds (default **60**) it re-reads the saved trains from `trackify.db` and applies
each one's saved configuration — connect, set the hub LED colour, and drive at its saved speed — and
**reconnects any hub that has dropped** since the last sweep. On Ctrl+C / SIGINT it stops every motor
and disconnects cleanly.

```bash
trackify auto                  # active saved trains, sweep every 60s
trackify auto --interval 30    # sweep every 30s
trackify auto --all            # include inactive trains too
```

New or edited trains are picked up automatically on the next sweep (the store is re-read each cycle).

## The train store (`trackify.db`)

Trains live in a **SQLite** database managed by **EF Core** (`SqliteTrainRepository`). Default path:
`~/.config/Trackify/trackify.db` (Linux) / `%APPDATA%\Trackify\trackify.db` (Windows), overridable
with the `TRACKIFY_STORE` environment variable. The schema is created automatically on first run;
enums are stored as readable names. Same schema as the app — copy the `.db` to the Pi, or point both
at the same file. The hub MAC (`HubId`/`BleAddress`) comes from `trackify discover`.

## Deploy to a Raspberry Pi

```bash
# From Windows for the Pi (arm64), self-contained (no .NET needed on the Pi):
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj -c Release -r linux-arm64 \
  --self-contained -o publish/
scp -r publish/ pi@raspberrypi:/opt/trackify/
ssh pi@raspberrypi 'chmod +x /opt/trackify/trackify'
```

No build flags needed even when cross-publishing from Windows: BlueZ is always compiled in, and
`AddLinuxLego` picks the real transport vs. the no-op fallback at **runtime** via
`OperatingSystem.IsLinux()` — so the same artifact works on the Pi. The CI `cli-arm64.yml` workflow
produces this artifact.

## Prerequisites on the Pi — install BlueZ

Trackify drives hubs by talking to **`bluetoothd` over D-Bus**, so the **BlueZ** stack must be
installed and running on the Pi. **Raspberry Pi OS** usually ships it; **Ubuntu Server** images do
**not** — you'll see `bluetoothctl: command not found` and `bluetooth.service could not be found`.

Run the bundled setup script once (Debian/Ubuntu). It's idempotent — safe to re-run:

```bash
sudo Source/Trackify.Cli/scripts/setup-bluez.sh   # or: sudo ./setup-bluez.sh from the scripts dir
```

It installs `bluez` (+ `rfkill`), enables & starts `bluetoothd`, unblocks and powers on the radio, and
adds your user to the `bluetooth` group. Equivalent manual steps:

```bash
sudo apt update && sudo apt install -y bluez rfkill
sudo systemctl enable --now bluetooth        # start bluetoothd now and at boot
sudo rfkill unblock bluetooth                # clear any soft-block
sudo usermod -aG bluetooth "$USER"           # D-Bus access for org.bluez (log out/in after)
bluetoothctl power on                         # power the adapter
```

**Log out & back in (or reboot)** after the group change so a non-root `trackify` isn't denied on
D-Bus. Then power on a hub and run `trackify discover` once so BlueZ knows the device. A handy
read-only checker, `Source/Trackify.Cli/scripts/pi-bt-info.sh`, dumps the full radio/adapter/scan
state if discovery still misbehaves.

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

`trackify drive` runs a single train until stopped; `trackify auto` runs **every** saved train and
re-scans on an interval. Both stop cleanly on SIGINT. For autostart, prefer `auto` for the whole fleet:

```ini
# /etc/systemd/system/trackify.service
[Unit]
Description=Trackify LEGO train control
After=bluetooth.target
Requires=bluetooth.service

[Service]
# One train: drive "Blauer Zug" --speed 40  ·  Whole fleet: auto --interval 60
ExecStart=/opt/trackify/trackify auto --interval 60
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

**Auf Windows (Entwickeln/Testen):** die `net10.0-windows…`-TFM bauen/starten (z. B. `dotnet run -f
net10.0-windows10.0.19041.0` oder die Rider-Run-Config) — sie nutzt **WinRT-Bluetooth**, also
funktionieren `discover`/`drive` auch am Dev-Rechner. Der plain `net10.0`-Build (der auf den Pi geht)
nutzt BlueZ unter Linux und meldet außerhalb Linux „Bluetooth is not available".

## Befehle

```bash
trackify                       # Dashboard: Banner, gespeicherte Züge, Befehlsübersicht
trackify list                  # gespeicherte Züge anzeigen
trackify discover              # nach Hubs scannen (Hub einschalten!), --timeout 15
trackify connect "Blauer Zug"  # Erreichbarkeits-Test (verbinden + trennen)
trackify drive   "Blauer Zug" --speed 40 --color Green   # fahren bis Ctrl+C
trackify stop    "Blauer Zug"  # Motor stoppen
trackify color   "Blauer Zug" Blue                        # Hub-LED setzen
trackify auto                  # Auto-Pilot: alle gespeicherten Züge fahren, per Intervall neu scannen
trackify --help                # vollständige Hilfe
```

Ein Zug wird per **Name oder Id** angesprochen (siehe `trackify list`).

## Auto-Modus (Auto-Pilot)

`trackify auto` ist eine Dauerschleife für den **unbeaufsichtigten Betrieb** auf dem Pi (systemd /
Docker). Alle `--interval` Sekunden (Standard **60**) liest es die gespeicherten Züge aus `trackify.db`
neu und wendet die gespeicherte Konfiguration jedes Zugs an — verbinden, Hub-LED setzen und mit der
gespeicherten Geschwindigkeit fahren — und **verbindet abgebrochene Hubs automatisch neu**. Bei
Ctrl+C / SIGINT werden alle Motoren gestoppt und sauber getrennt.

```bash
trackify auto                  # aktive gespeicherte Züge, Scan alle 60s
trackify auto --interval 30    # Scan alle 30s
trackify auto --all            # auch inaktive Züge einschließen
```

Neue oder geänderte Züge werden beim nächsten Durchlauf automatisch übernommen (der Store wird jedes
Mal neu gelesen).

## Der Train-Store (`trackify.db`)

Züge liegen in einer **SQLite**-Datenbank, verwaltet von **EF Core** (`SqliteTrainRepository`). Standardpfad:
`~/.config/Trackify/trackify.db` (Linux) / `%APPDATA%\Trackify\trackify.db` (Windows), überschreibbar
per Umgebungsvariable `TRACKIFY_STORE`. Das Schema wird beim ersten Start automatisch angelegt; Enums
werden als lesbare Namen gespeichert. Gleiches Schema wie die App — die `.db` auf den Pi kopieren oder
beide auf dieselbe Datei zeigen lassen. Die Hub-MAC (`HubId`/`BleAddress`) liefert `trackify discover`.

## Auf einen Raspberry Pi deployen

```bash
# Von Windows aus für den Pi (arm64), self-contained (kein .NET auf dem Pi nötig):
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj -c Release -r linux-arm64 \
  --self-contained -o publish/
scp -r publish/ pi@raspberrypi:/opt/trackify/
ssh pi@raspberrypi 'chmod +x /opt/trackify/trackify'
```

Kein Build-Flag nötig, auch beim Cross-Publish von Windows: BlueZ ist immer einkompiliert, und
`AddLinuxLego` wählt zur **Laufzeit** per `OperatingSystem.IsLinux()` den echten Transport bzw. den
No-op-Fallback — dasselbe Artefakt läuft also auf dem Pi. Der CI-Workflow `cli-arm64.yml` erzeugt
dieses Artefakt.

## Voraussetzungen auf dem Pi — BlueZ installieren

Trackify steuert Hubs über **`bluetoothd` per D-Bus**, also muss der **BlueZ**-Stack auf dem Pi
installiert sein und laufen. **Raspberry Pi OS** bringt ihn meist mit; **Ubuntu Server**-Images
**nicht** — dann erscheint `bluetoothctl: command not found` und `bluetooth.service could not be found`.

Das mitgelieferte Setup-Skript einmal ausführen (Debian/Ubuntu). Es ist idempotent — beliebig oft
wiederholbar:

```bash
sudo Source/Trackify.Cli/scripts/setup-bluez.sh
```

Es installiert `bluez` (+ `rfkill`), aktiviert & startet `bluetoothd`, entsperrt und schaltet das Radio
ein und fügt den Benutzer der `bluetooth`-Gruppe hinzu. Manuell entspricht das:

```bash
sudo apt update && sudo apt install -y bluez rfkill
sudo systemctl enable --now bluetooth
sudo rfkill unblock bluetooth
sudo usermod -aG bluetooth "$USER"     # danach ab-/anmelden
bluetoothctl power on
```

Nach der Gruppenänderung **ab- und wieder anmelden (oder neu starten)**, damit ein Nicht-Root-
`trackify` auf D-Bus nicht abgewiesen wird. Dann einen Hub einschalten und einmal `trackify discover`
ausführen. Das read-only-Prüfskript `Source/Trackify.Cli/scripts/pi-bt-info.sh` zeigt den kompletten
Radio-/Adapter-/Scan-Status, falls die Erkennung weiter hakt.

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
