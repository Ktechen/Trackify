# The UI: which page is for what, and what every setting does

[Deutsch](editor-view.de.md) · **English**

Operator-facing, not architectural. Where code belongs is in
[arc42 §05](arc42/05-building-block-view.md); why a track plan is a graph of parts and ports
rather than a list of coordinates is [ADR-17](arc42/09-architecture-decisions.md).

**The labels quoted below are German, because the app's UI is German** (that is the repo
convention — the CLI speaks English, the app speaks German). Each one is followed by what it
means, so this file is usable without reading German.

> **About the screenshots.** They are **not taken yet** — the slots below are empty. The
> Skia-rendered UI cannot be captured in the dev environment: the GPU surface comes back blank and
> the WASM canvas times out the browser tool (`CLAUDE.md` records exactly this). So the captures
> have to come from **a real device**. Each slot names the file it expects; drop the image into
> `docs/images/` and it appears here. Capture instructions at the end.

---

## The two pages

| Page | For | In short |
|---|---|---|
| **Train-Konfiguration** — train configuration (`MainPage`) | Creating trains, discovering and connecting hubs, per-train driving behaviour | *What* drives |
| **Streckenplaner** — track planner (`SecondPage`) | Drawing the layout, configuring parts, placing sensors | *Where* it drives |

Getting between them: the track-icon button in the train page's header
("Streckenplaner öffnen" — open the track planner), and the back arrow in the planner
("Zurück zur Train-Konfiguration" — back to train configuration).

Both pages are **responsive**. In the wide layout the areas sit side by side; in the narrow one
they stack and the detail panel appears only while something is selected.

---

## Track planner (Streckenplaner)

![Track planner, overview](images/streckenplaner-uebersicht.png)

*Expected: `docs/images/streckenplaner-uebersicht.png` — wide layout, oval loaded, one part selected.*

### The principle in one sentence

The hint on the canvas says it itself: **"Zieh von einem leuchtenden Punkt — die Strecke entsteht
aus echten LEGO-Teilen."** — *drag from a glowing dot; the track is made of real LEGO parts.*
While you drag, a dashed preview built from real parts follows your finger; releasing commits it.
Building needs **no setting at all** — speed, direction and sensors come later, when you tap a
part that is already down.

### The anchor dots

Every open track end is a large dot, and it does two things:

- **Dragging away from it** builds track.
- **Tapping it** makes it the target of the buttons under "Am gewählten Punkt anhängen"
  (*append at the selected point*).

Drag near *another* open end and the planner searches for the run of parts that actually reaches
it, and closes the gap. An empty plan places a straight automatically, so there are two dots to
drag from in the first place.

### The toolbar

![Toolbar](images/streckenplaner-werkzeugleiste.png)

*Expected: `docs/images/streckenplaner-werkzeugleiste.png` — the three button rows below the canvas.*

**First row — what rescues a misstep**

| Button | Effect |
|---|---|
| **Zurück** (*undo*) | Takes back the last building step: placing, deleting, templates. **Not** the inspector's sliders — those you simply set back. |
| *(arrow, "Wiederholen" — redo)* | Redoes what was undone. |
| **Kreis schließen** (*close the loop*) | Tooltip: "Die fehlenden Teile einsetzen, bis der Kreis zu ist" — *insert the missing parts until the loop is closed*. Only enabled when two open ends are reachably close; the search budget is ten parts. |
| **Teil löschen** (*delete part*) | Removes the selected part along with its connections. |

**Second row — "Am gewählten Punkt anhängen" (append at the selected point)**

`Gerade` (straight) · `Kurve links` / `Kurve rechts` (curve left / right) · `Bahnhof` (station) ·
`Weiche links` / `Weiche rechts` (point/switch left / right) · `Kreuzung` (crossing)

A **strip that scrolls sideways**, not a fixed row: as a fixed row it measures some 755 px, and on a
phone everything past the second button would be unreachable. The order is therefore most-used
first — what you rarely need sits at the far right.

Straights and curves can also be drawn. **Points and crossings deliberately cannot**: a swipe must
not guess a switch — you place those on purpose.

**Third row — templates**

`Oval` (a closed loop of real parts) · `Strecke` (*line* — point to point between two stations) ·
`Leeren` (*clear*).

### What the canvas tells you

| Indication | Meaning |
|---|---|
| **Track colour** | The speed. The "Tempo" (*speed*) legend bottom-left runs red = stop to green = fast — hidden in the narrow layout. |
| **Number on the track** | That part's maximum speed, in percent. |
| **Dashed line** | The preview while dragging — not committed, not counted yet. |
| **Marker "F"** | Colour sensor on this part (*Farbe* = colour). |
| **Marker "A"** | Distance sensor (*Abstand* = distance). |
| **Red message** | A connection does not fit geometrically, with the gap in studs. Shown only on failure — a store error surfaces here too, rather than vanishing. |
| **Header** | `n Teile · n Sensoren · n offen` — parts, sensors, open ends. |

Top right: **Vergrößern / Verkleinern / Einpassen** (zoom in / out / fit). The zoom is a value the
ViewModel owns driving a `Viewbox`, not a `ScrollViewer` zoom — which is why it behaves the same on
every target.

### The inspector — the per-part settings

![Inspector](images/streckenplaner-inspector.png)

*Expected: `docs/images/streckenplaner-inspector.png` — a selected part with a sensor set.*

With nothing selected it reads **"Kein Segment ausgewählt."** — *no segment selected*. Tap a part
and three blocks appear:

**Fahrverhalten — driving behaviour**

| Setting | What it does |
|---|---|
| **Max. Geschwindigkeit** (0–100 %) — maximum speed | Top speed across this part. Also colours the track on the canvas. |
| **Fahrtrichtung** — direction of travel: `Vorwärts` / `Rückwärts` (forward / reverse) | The direction this part is traversed in. |
| **Geschwindigkeitsprofil f(x)** — speed profile | Two pickers, **Beschleunigung** (acceleration) and **Bremsen** (braking): linear, EaseIn, EaseOut, SCurve, Exponential, Custom. The curve below shows the result, the text below it the formula. `Custom` deliberately carries no fixed formula. |

**Weiche — point/switch** *(only on switch parts)*

| Setting | What it does |
|---|---|
| **Gestellt auf** — set to: `Stammgleis` / `Zweig` (main route / diverging route) | The position this switch holds in the plan. |

That block also carries the sentence that names the boundary: *"Welcher Motor diese Weiche stellt
und wo ihr Sensor hängt, trägt der Streckenplan schon mit und wandert beim Export mit. Einstellbar
wird es hier, wenn die Ansteuerung kommt."* — **which motor throws this switch and where its
sensor sits is already carried by the track plan and travels with the export; it becomes editable
here once actuation lands.** **Where** the branch leads is not a setting but geometry: whether a
`Weiche links` or `Weiche rechts` is placed, and what is plugged into its branch port.

**Sensor**

| Setting | What it does |
|---|---|
| **Typ** — type: `Kein` / `Farbe` / `Abstand` (none / colour / distance) | Which sensor sits on this part. |
| **Aktion beim Auslösen** — action on trigger | What happens when it fires. |
| **Ziel-Tempo** (%) — target speed | Shown only when the action slows the train down: what it slows to. |

**Teil löschen** (*delete part*) closes the block — the same command as in the toolbar, but within
reach of the hand that is already in the inspector.

### The parts list

Header: `n Teile · n offene Enden` — parts, open ends. Below it every placed part with its speed
colour chip, name, type and a delete button. Tapping one selects it and fills the inspector.

**No sorting, no reordering, no duplicating.** In a graph a part has ports, not a position in a
chain — the file says so itself: *"in a graph a part has no position in a chain, so there is
nothing to reorder — this is a parts list, not a sequence."*

### Layout as text

```
┌──────────────────────────────────────────────────────────────────────┐
│ ← │ 🚆 │ Streckenplaner       12 Teile 2 Sensoren 3 offen │Speichern│
├──────────────────────────────┬──────────────┬────────────────────────┤
│                              │ n Teile      │ Fahrverhalten          │
│   drawing canvas             │ n offen      │  Max. Geschw. ───○──   │
│   • anchor dots              ├──────────────┤  Fahrtrichtung         │
│   • track, coloured by       │ ■ Gerade 1   │   [Vorwärts][Rückw.]   │
│     speed, with its number   │ ■ Kurve r. 1 │  Profil f(x)  ╱▔▔╲     │
│   • dashed preview           │ ■ Weiche 1   │ Weiche                 │
│   • F / A — sensor markers   │ ■ Bahnhof 1  │  [Stammgleis][Zweig]   │
│                              │              │ Sensor                 │
│  ┌ Tempo ─────┐    [+][-][⤢] │              │  [Kein][Farbe][Abst.]  │
├──────────────────────────────┴──────────────┴────────────────────────┤
│ [Zurück][↷][Kreis schließen][Teil löschen]                           │
│ Am gewählten Punkt anhängen                                          │
│ [Gerade][Kurve l.][Kurve r.][Weiche l.][Weiche r.][Kreuzung][Bahnhof]│
│ [Oval][Strecke][Leeren]                                              │
└──────────────────────────────────────────────────────────────────────┘
```

The dividers between canvas, list and inspector are **draggable** (240–420 px for the list,
320–560 px for the inspector).

### On a phone

Below 720 px of width — a phone, or a window dragged narrow — the page switches over. The canvas
keeps the **whole** area and both side panes dock over it as sheets:

| | Wide | Narrow |
|---|---|---|
| **Parts list** | Column beside the canvas | Bottom sheet, opened by the header button showing the part count |
| **Inspector** | Its own column | Bottom sheet, while a part is selected |
| **First-row buttons** | Icon **and** word | Icon only (~190 px instead of ~390) — the wording moves into the tooltip |
| **Palette** | All seven visible | The same strip, scrolled sideways |
| **Header** | Emblem, subtitle, three counts | Title and Speichern only, plus the parts button |
| **Speed legend** | Bottom left | Hidden — the track's colour and number say the same thing |

**One sheet at a time**: select a part and the inspector wins, the parts list steps aside. The parts
list starts closed — you build first and inspect afterwards.

Stacked, the parts list used to take two of five rows, which left the canvas roughly 240 px once the
header and toolbar had taken their share. That is why it is a sheet now.

---

## Train configuration (Train-Konfiguration)

![Train configuration](images/train-konfiguration.png)

*Expected: `docs/images/train-konfiguration.png` — train list left, train editor right.*

Header: **"Train-Konfiguration"** with the subtitle **"Powered Up · Legoino-Schnittstelle"**
(*Legoino interface*) and the count `n aktiv · n gesamt` (active, total).

| Area | For |
|---|---|
| **Train list** | Every train; "Zurück zur Liste" (*back to the list*) returns there in the narrow layout. The search box filters by name and hub type. |
| **Train editor** | Name, hub model, BLE address, ports, speed, colour, acceleration and braking function, active yes/no. |
| **Hub suchen** (*discover hubs*) | Scans for hubs. Found ones are offered **for confirmation**, not adopted silently; if a train already knows the address, the hub binds to it instead of creating a second train. |
| **Verbinden / Trennen** (*connect / disconnect*) | Opens the BLE connection — by the device id from discovery, otherwise by the typed-in BLE address. |

![Connection panel](images/verbindung-panel.png)

*Expected: `docs/images/verbindung-panel.png` — the panel open, server mode selected.*

The **"Verbindung (Direkt/Server)"** button (*connection, direct/server*) opens the **"Verbindung"**
panel. It states the difference plainly: *"Servermodus: Steuerung über einen Trackify-Backend
(Raspberry Pi). Aus = Direktmodus über das Bluetooth dieses Geräts."* — **server mode drives through
a Trackify backend on a Raspberry Pi; off means direct mode over this device's own Bluetooth.** The
address field shows `http://192.168.1.50:5000` as its example. Switching takes effect
**immediately**, with no restart.

---

## Taking the screenshots

Five images, into `docs/images/`:

| File | Content |
|---|---|
| `streckenplaner-uebersicht.png` | Wide layout, oval loaded, one part selected |
| `streckenplaner-werkzeugleiste.png` | The three button rows only |
| `streckenplaner-inspector.png` | Inspector with a sensor set and the switch block visible |
| `train-konfiguration.png` | Train list and train editor |
| `verbindung-panel.png` | Connection panel open, server mode |

Start the desktop head:

```bash
dotnet run --project Source/Trackify/Trackify.csproj -f net10.0-desktop
```

Capture with the Snipping Tool (`Win`+`Shift`+`S`).

For the narrow layout — stacked areas, detail panel only while something is selected — drag the
desktop window narrow, or take the APK from `android-apk.yml` and capture on the device. There you
also see how the system bars are kept clear via `utu:SafeArea`.
