# Trackify CLI

Steuert LEGO Powered Up Hubs über das **Onboard-Bluetooth (BlueZ)** eines Linux-Rechners —
gedacht für einen Raspberry Pi / Linux-Server. Teilt sich Domain/Application/Infrastructure
(und die `trains.json`) mit der Trackify-App.

**Auf Windows (Entwickeln/Testen):** Der Windows-Build der CLI (`net10.0-windows…`-TFM, z. B. via
Rider-Run-Config oder `dotnet run -f net10.0-windows10.0.19041.0`) nutzt **WinRT-Bluetooth** —
`discover`/`drive` funktionieren also auch direkt am Dev-Rechner. Der plain `net10.0`-Build hat
bewusst keinen Windows-Transport und meldet „Bluetooth is not available".

## Befehle

```bash
trackify                       # Dashboard: Banner, gespeicherte Trains, Befehls-Übersicht
trackify list                  # gespeicherte Trains anzeigen
trackify discover              # nach Hubs scannen (Hub einschalten!), --timeout 15
trackify connect "Blauer Zug"  # Erreichbarkeits-Test (verbinden + trennen)
trackify drive   "Blauer Zug" --speed 40 --color Green   # fahren bis Ctrl+C
trackify stop    "Blauer Zug"  # Motor stoppen
trackify color   "Blauer Zug" Blue                        # Hub-LED setzen
trackify --help                # vollständige Hilfe
```

Ein Train wird per **Name oder Id** aus dem Store angesprochen (siehe `trackify list`).

## Der Train-Store (`trains.json`)

Standardpfad: `~/.config/Trackify/trains.json` (Linux) bzw. `%APPDATA%\Trackify\trains.json` (Windows).
Überschreibbar per Umgebungsvariable **`TRACKIFY_STORE`**. Gleiches Schema wie die App — Datei
von der App auf den Pi kopieren oder von Hand anlegen:

```json
[
  {
    "Id": "trn-1",
    "Name": "Blauer Zug",
    "Hub": "PoweredUpHub",
    "BleAddress": "90:84:2B:11:22:33",
    "HubId": "90:84:2B:11:22:33",
    "Color": "Blue",
    "PortA": "TrainMotor",
    "PortB": "None",
    "Speed": 60,
    "AccelFn": "EaseOut",
    "AccelExpression": "1-(1-x)^2",
    "BrakeFn": "EaseIn",
    "BrakeExpression": "1-(1-x)^2",
    "IsActive": true
  }
]
```

`HubId`/`BleAddress` ist die MAC des Hubs — einmal `trackify discover` zeigt sie an.

## Auf den Raspberry Pi deployen

**Wichtig:** Das `LINUX`-Compile-Flag entscheidet, ob der echte BlueZ-Transport einkompiliert
wird. Auf einem Linux-Host ist es automatisch an; beim **Cross-Publish von Windows** muss es
explizit gesetzt werden:

```bash
# Von Windows aus für den Pi (ARM64), self-contained (kein .NET auf dem Pi nötig):
dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj -c Release -r linux-arm64 \
  --self-contained -p:TrackifyLinux=true -o publish/

# Auf den Pi kopieren:
scp -r publish/ pi@raspberrypi:/opt/trackify/
ssh pi@raspberrypi 'chmod +x /opt/trackify/trackify'
```

Voraussetzungen auf dem Pi: BlueZ läuft (`systemctl status bluetooth`), und der Benutzer ist
in der `bluetooth`-Gruppe (`sudo usermod -aG bluetooth pi`). Vor dem ersten `connect` einmal
`trackify discover` laufen lassen, damit BlueZ das Gerät kennt.

## Alternative: Docker

Aus dem Repo-Root (Compose-Datei liegt dort):

```bash
docker compose up -d                       # bauen + dauerhaft starten (Autostart nach Reboot)
docker compose logs -f                     # Serilog-Ausgabe live
docker compose run --rm trackify discover  # Einmal-Befehle: discover, list, stop, color …
docker compose down                        # stoppt den Zug sauber (SIGINT)
```

Was das Compose-Setup regelt (siehe `docker-compose.yml`):
- **`network_mode: host` + `/var/run/dbus`-Mount** — BLE läuft über den `bluetoothd` des **Hosts**;
  der Container spricht nur dessen D-Bus. BlueZ muss also auf dem Pi selbst laufen.
- **`stop_signal: SIGINT`** — `docker compose down` wirkt wie Ctrl+C: Motor stoppen + sauber trennen.
- **`./data:/data`** — dort liegt die `trains.json` (`TRACKIFY_STORE=/data/trains.json`).
- **`restart: unless-stopped`** — Neustart bei Absturz und nach dem Booten.
- Der Fahr-Befehl (Zugname/Speed) steht unter `command:` und wird dort angepasst.

Da das Image **im Linux-Container gebaut** wird, ist das `LINUX`-Flag automatisch an — der echte
BlueZ-Transport ist einkompiliert, ganz ohne `-p:TrackifyLinux=true`. Das Basis-Image ist
multi-arch; auf dem Pi zieht Docker automatisch die arm64-Variante.

## Dauerbetrieb: automatisch beim Booten starten (systemd)

`trackify drive` läuft bereits dauerhaft (bis Ctrl+C, dann Motor-Stopp + sauberes Trennen).
Für den Autostart beim Booten macht man daraus einen systemd-Dienst:

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
Environment=TRACKIFY_STORE=/home/pi/.config/Trackify/trains.json
# SIGINT = Ctrl+C: löst unser sauberes Herunterfahren aus (Motor stoppen + trennen).
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now trackify   # startet jetzt UND bei jedem Boot
journalctl -u trackify -f              # Serilog-Ausgabe live ansehen
sudo systemctl stop trackify           # stoppt den Zug sauber (SIGINT)
```

`Restart=on-failure` startet den Dienst neu, wenn die Verbindung abreißt und der Prozess
mit Fehler endet.
