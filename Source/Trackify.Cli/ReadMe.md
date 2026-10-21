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

## Install on a Raspberry Pi — the `.deb`

Every `v*` release carries `trackify_<version>_arm64.deb` on the
[Releases page](https://github.com/Ktechen/Trackify/releases). It is the quickest route to a working
Pi, and the only one that also deals with the prerequisites:

```bash
sudo apt install ./trackify_1.0.0_arm64.deb
```

That one command:

- unpacks the self-contained build to `/usr/lib/trackify` and puts `trackify` on `PATH` — **no .NET
  runtime needed on the Pi**;
- **pulls in `bluez`** through `Depends:`, so the stack Trackify talks to over D-Bus is installed and
  running — the step Ubuntu Server images otherwise leave to you;
- creates the `trackify` system user and **adds it to the `bluetooth` group** (BlueZ grants
  `org.bluez` to that group only — the usual cause of "discovery finds nothing");
- creates the store directory `/var/lib/trackify`; and
- installs the systemd unit.

Then:

```bash
trackify discover                     # find your hubs (turn one on first)
sudo systemctl enable --now trackify  # run the whole fleet, now and at boot
```

**The service is installed but deliberately not started.** `apt install` has no business setting a
physical layout in motion, so enabling it stays an explicit step. The unit runs as the `trackify`
user against `/var/lib/trackify/trackify.db` (`TRACKIFY_STORE` in
`/lib/systemd/system/trackify.service`).

`apt remove` stops the service cleanly (SIGINT, so no train is left moving) and keeps your trains;
`apt purge` also deletes `/var/lib/trackify`.

To build the package yourself — exactly what the release workflow runs:

```bash
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj \
  -f net10.0 -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
.github/scripts/build-deb.sh --version 1.0.0 --publish-dir publish/linux-arm64 --output dist
```

`dpkg-deb` is Linux-only, so build the package on Linux or in CI. `--stage-only` just lays the
package tree out for inspection and works anywhere (on Windows the mode bits and the
`/usr/bin/trackify` symlink will look wrong there — both are correct when built on Linux).

**Why a `.deb` rather than a tarball, a single-file binary, or only Docker?** A tarball still leaves
BlueZ, the service user, the group membership and the unit file to do by hand — the four steps the
package removes. [Docker](#docker) remains the right answer if you already run containers on the Pi,
but it needs a runtime plus host concessions (`network_mode: host`, a D-Bus mount, the `bluetooth`
GID). The raw `linux-arm64` directory is still published with every release for non-Debian Linux,
where `apt` is not an option — that is the manual route below.

## Deploy to a Raspberry Pi by hand (any Linux)

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

The container runs as the **non-root** `app` user (UID 1654) that the .NET base image provides, so two
host-side things have to line up once, before the first `up`:

```bash
# 1. The store is a host bind mount and keeps its host ownership — make it writable by UID 1654.
mkdir -p data && sudo chown -R 1654:1654 data

# 2. BlueZ grants org.bluez over D-Bus to root and to the `bluetooth` group only, so the container
#    must join that group by its HOST GID. Usually 113 — check yours:
export BLUETOOTH_GID=$(getent group bluetooth | cut -d: -f3)
```

Then, from the repo root:

```bash
docker compose up -d                       # build + run permanently (auto-restart after reboot)
docker compose logs -f                     # live Serilog output
docker compose run --rm trackify discover  # one-shot commands
docker compose down                        # stops the train cleanly (SIGINT)
```

`docker-compose.yml` uses host networking + a `/var/run/dbus` mount (BLE via the host's `bluetoothd`),
`group_add` for the bluetooth GID, `stop_signal: SIGINT` for a clean stop, and `restart: unless-stopped`.
Building inside the Linux container turns the LINUX flag on automatically, so real BlueZ is compiled in.

**If discovery finds nothing or the store can't be written**, the non-root switch is the first thing to
suspect — check both steps above. `docker compose logs` shows a D-Bus `AccessDenied` for the group
problem and a permission error on `/data` for the ownership one. To rule it out quickly, add
`user: "0:0"` to the `trackify` service to run as root again as before.

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

## Auf einem Raspberry Pi installieren — das `.deb`

Jedes `v*`-Release enthält `trackify_<version>_arm64.deb` auf der
[Releases-Seite](https://github.com/Ktechen/Trackify/releases). Das ist der schnellste Weg zu einem
laufenden Pi — und der einzige, der auch die Voraussetzungen mitbringt:

```bash
sudo apt install ./trackify_1.0.0_arm64.deb
```

Dieser eine Befehl:

- entpackt den self-contained Build nach `/usr/lib/trackify` und legt `trackify` in den `PATH` —
  **kein .NET-Runtime auf dem Pi nötig**;
- zieht per `Depends:` **`bluez`** mit, damit der Stack, mit dem Trackify über D-Bus spricht,
  installiert und gestartet ist — genau der Schritt, den Ubuntu-Server-Images einem sonst überlassen;
- legt den System-Benutzer `trackify` an und **nimmt ihn in die Gruppe `bluetooth` auf** (BlueZ gibt
  `org.bluez` nur dieser Gruppe frei — die übliche Ursache für „die Erkennung findet nichts");
- legt das Store-Verzeichnis `/var/lib/trackify` an; und
- installiert die systemd-Unit.

Danach:

```bash
trackify discover                     # Hubs finden (vorher einen einschalten)
sudo systemctl enable --now trackify  # ganze Flotte fahren, jetzt und bei jedem Boot
```

**Der Dienst wird installiert, aber absichtlich nicht gestartet.** `apt install` hat nicht die
Aufgabe, eine echte Anlage in Bewegung zu setzen — das Aktivieren bleibt ein eigener, bewusster
Schritt. Die Unit läuft als Benutzer `trackify` gegen `/var/lib/trackify/trackify.db`
(`TRACKIFY_STORE` in `/lib/systemd/system/trackify.service`).

`apt remove` stoppt den Dienst sauber (SIGINT, es bleibt also kein Zug fahren) und behält die Züge;
`apt purge` löscht zusätzlich `/var/lib/trackify`.

Das Paket selbst bauen — genau das macht der Release-Workflow:

```bash
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj \
  -f net10.0 -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
.github/scripts/build-deb.sh --version 1.0.0 --publish-dir publish/linux-arm64 --output dist
```

`dpkg-deb` gibt es nur unter Linux, das Paket also unter Linux oder in der CI bauen. `--stage-only`
legt nur den Paketbaum zum Ansehen an und läuft überall (unter Windows sehen dort die Rechte-Bits und
der Symlink `/usr/bin/trackify` falsch aus — unter Linux gebaut sind beide korrekt).

**Warum ein `.deb` und nicht ein Tarball, ein Single-File-Binary oder nur Docker?** Ein Tarball
ließe BlueZ, den Service-Benutzer, die Gruppenmitgliedschaft und die Unit-Datei weiter von Hand
übrig — die vier Schritte, die das Paket abnimmt. [Docker](#docker) bleibt die richtige Antwort, wenn
auf dem Pi ohnehin Container laufen, braucht aber eine Container-Runtime und Zugeständnisse auf dem
Host (`network_mode: host`, D-Bus-Mount, die `bluetooth`-GID). Das rohe `linux-arm64`-Verzeichnis
wird weiter mit jedem Release veröffentlicht — für Nicht-Debian-Linux, wo `apt` keine Option ist:
das ist der manuelle Weg unten.

## Auf einen Raspberry Pi deployen (von Hand, jedes Linux)

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

Der Container läuft als **Nicht-Root**-Benutzer `app` (UID 1654) aus dem .NET-Basisimage. Dafür müssen
einmalig zwei Dinge auf dem Host passen, vor dem ersten `up`:

```bash
# 1. Der Store ist ein Host-Bind-Mount und behält die Host-Rechte — für UID 1654 beschreibbar machen.
mkdir -p data && sudo chown -R 1654:1654 data

# 2. BlueZ erlaubt org.bluez über D-Bus nur root und der Gruppe `bluetooth`; der Container muss dieser
#    Gruppe per HOST-GID beitreten. Meist 113 — die eigene ermitteln:
export BLUETOOTH_GID=$(getent group bluetooth | cut -d: -f3)
```

Danach aus dem Repo-Root:

```bash
docker compose up -d                       # bauen + dauerhaft starten (Autostart nach Reboot)
docker compose logs -f                     # Serilog-Ausgabe live
docker compose run --rm trackify discover  # Einmal-Befehle
docker compose down                        # stoppt den Zug sauber (SIGINT)
```

`docker-compose.yml` nutzt Host-Networking + `/var/run/dbus`-Mount (BLE über den `bluetoothd` des
Hosts), `group_add` für die Bluetooth-GID, `stop_signal: SIGINT` für sauberes Stoppen und
`restart: unless-stopped`. Da im Linux-Container gebaut wird, ist das LINUX-Flag automatisch an —
echtes BlueZ ist einkompiliert.

**Wenn die Erkennung nichts findet oder der Store nicht geschrieben werden kann**, ist zuerst die
Nicht-Root-Umstellung zu prüfen — beide Schritte oben. `docker compose logs` zeigt bei der Gruppe ein
D-Bus-`AccessDenied`, bei den Rechten einen Permission-Fehler auf `/data`. Zum schnellen Ausschließen
`user: "0:0"` beim Service `trackify` ergänzen — dann läuft er wie bisher als root.

## Dauerbetrieb beim Booten (systemd)

`trackify drive` läuft bereits bis zum Stopp (Ctrl+C → Motor-Stopp + sauberes Trennen). Für Autostart
die systemd-Unit oben (englischer Teil) verwenden:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now trackify   # startet jetzt UND bei jedem Boot
journalctl -u trackify -f              # Logs live
sudo systemctl stop trackify           # stoppt den Zug sauber (SIGINT)
```
