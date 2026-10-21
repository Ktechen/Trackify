# Die Oberfläche: welche Seite wofür, und was jede Einstellung tut

**Deutsch** · [English](editor-view.md)

Bedienungsorientiert, nicht architektonisch. Wo Code hingehört, steht in
[arc42 §05](arc42/05-building-block-view.md); warum ein Streckenplan ein Graph aus Teilen und
Ports ist und keine Koordinatenliste, in [ADR-17](arc42/09-architecture-decisions.md).

> **Zu den Screenshots.** Die Bilder sind **noch nicht aufgenommen** — die Plätze unten sind leer.
> Die Skia-gerenderte Oberfläche lässt sich in der Entwicklungsumgebung nicht abgreifen: die
> GPU-Fläche kommt leer zurück, und der WASM-Canvas treibt das Browser-Werkzeug in Timeouts
> (`CLAUDE.md` hält genau das fest). Die Aufnahmen müssen daher **von einem echten Gerät**
> kommen. Jeder Platz nennt den erwarteten Dateinamen; leg das Bild nach `docs/images/` und es
> erscheint hier automatisch. Aufnahme-Anleitung am Ende.

---

## Die zwei Seiten

| Seite | Wofür | Kurz |
|---|---|---|
| **Train-Konfiguration** (`MainPage`) | Züge anlegen, Hubs suchen und verbinden, Fahrverhalten pro Zug | *Was* fährt |
| **Streckenplaner** (`SecondPage`) | Die Strecke zeichnen, Teile konfigurieren, Sensoren setzen | *Wo* es fährt |

Hin und zurück: im Kopf der Train-Seite der Knopf mit dem Gleis-Symbol
(„Streckenplaner öffnen"), im Streckenplaner der Zurück-Pfeil („Zurück zur Train-Konfiguration").

Beide Seiten sind **responsiv**. Im Breiten-Layout stehen die Bereiche nebeneinander. Im schmalen
zeigt die Train-Seite die Liste *oder* den Editor, und der Streckenplaner legt Teileliste und
Inspector als Laschen über die Zeichenfläche. In beiden Kopfzeilen kürzt sich dann der Titel, damit
die Knöpfe rechts nie aus dem Bild rutschen.

---

## Streckenplaner

![Streckenplaner, Gesamtansicht](images/streckenplaner-uebersicht.png)

*Erwartet: `docs/images/streckenplaner-uebersicht.png` — breites Layout, Oval geladen, ein Teil gewählt.*

### Das Prinzip in einem Satz

Der Hinweis auf der Fläche sagt es selbst: **„Zieh von einem leuchtenden Punkt — die Strecke
entsteht aus echten LEGO-Teilen."** Während des Ziehens folgt eine gestrichelte Vorschau aus
echten Teilen dem Finger; Loslassen legt sie hin. Bauen braucht **keine einzige Einstellung** —
Tempo, Richtung und Sensoren kommen erst, wenn du ein gelegtes Teil antippst.

### Die Andockpunkte

Jedes offene Gleisende ist ein großer Punkt, und er kann zweierlei:

- **Von ihm wegziehen** baut Strecke.
- **Antippen** macht ihn zum Ziel der Knöpfe unter „Am gewählten Punkt anhängen".

Ziehst du in die Nähe eines *anderen* offenen Endes, sucht der Planer die Teilefolge, die es
wirklich erreicht, und schließt die Lücke. Ein leerer Plan setzt automatisch eine Gerade, damit
überhaupt zwei Punkte zum Ziehen da sind.

### Die Werkzeugleiste

![Werkzeugleiste](images/streckenplaner-werkzeugleiste.png)

*Erwartet: `docs/images/streckenplaner-werkzeugleiste.png` — die drei Knopfreihen unter der Fläche.*

**Erste Reihe — was einen Fehlgriff rettet**

| Knopf | Wirkung |
|---|---|
| **Rückgängig** | Nimmt den letzten Bau-Schritt zurück: Legen, Löschen, Vorlagen. **Nicht** die Schieberegler im Inspector — die stellt man einfach zurück. Heißt bewusst nicht „Zurück": das ist der Pfeil im Kopf, und der verlässt die Seite. |
| *(Pfeil, „Wiederholen")* | Macht das Zurückgenommene wieder. |
| **Kreis schließen** | „Die fehlenden Teile einsetzen, bis der Kreis zu ist." Nur aktiv, wenn zwei offene Enden erreichbar nah sind — das Suchbudget liegt bei zehn Teilen. |
| **Teil löschen** | Entfernt das gewählte Teil samt seinen Verbindungen. |

**Zweite Reihe — „Am gewählten Punkt anhängen"**

`Gerade` · `Kurve links` · `Kurve rechts` · `Bahnhof` · `Weiche links` · `Weiche rechts` · `Kreuzung`

Eine **seitlich wischbare Leiste**, keine feste Reihe: als feste Reihe misst sie rund 755 px, und auf
dem Handy wäre alles ab dem zweiten Knopf unerreichbar. Die Reihenfolge ist deshalb
„häufigstes zuerst" — was du selten brauchst, liegt rechts außen.

Gerade und Kurven lassen sich auch ziehen. **Weiche und Kreuzung bewusst nicht**: eine Wischgeste
darf keine Weiche erraten — die setzt man absichtlich.

**Dritte Reihe — Vorlagen**

`Oval` (geschlossene Schleife aus echten Teilen) · `Strecke` (Punkt zu Punkt zwischen zwei
Bahnhöfen) · `Leeren`.

### Was die Fläche dir sagt

| Anzeige | Bedeutung |
|---|---|
| **Farbe des Gleises** | Das Tempo. Legende „Tempo" unten links, eine Stufe je Gleisfarbe: „Stopp" (rot), „sehr langsam", „langsam", „mittel", „schnell" (grün) — im Schmal-Layout ausgeblendet. |
| **Zahl am Gleis** | Das Höchsttempo dieses Teils in Prozent. |
| **Gestrichelte Linie** | Vorschau während des Ziehens — noch nicht gelegt, zählt noch nicht mit. |
| **Marke mit „F"** | Farbsensor auf diesem Teil. |
| **Marke mit „A"** | Abstandssensor. |
| **Rote Meldung** | Eine Verbindung passt geometrisch nicht, mit der Lücke in Stud. Erscheint nur im Fehlerfall — auch ein Store-Fehler landet hier statt zu verschwinden. |
| **Kopfzeile** | `n Teile · n Sensoren · n offen`. |

Rechts oben **Vergrößern / Verkleinern / Einpassen**. Der Zoom ist ein Wert, den das ViewModel
hält (eine `Viewbox`), kein `ScrollViewer`-Zoom — deshalb verhält er sich auf allen Zielen gleich.

### Der Inspector — die Einstellungen pro Teil

![Inspector](images/streckenplaner-inspector.png)

*Erwartet: `docs/images/streckenplaner-inspector.png` — ein gewähltes Teil mit gesetztem Sensor.*

Ohne Auswahl steht hier **„Kein Teil ausgewählt. Tippe ein Teil auf der Fläche an."** Angetippt
erscheinen drei Blöcke. Oben rechts schließt der **×-Knopf** („Auswahl schließen") den Inspector
wieder — auf dem Handy der Weg, die Lasche von der Zeichenfläche zu nehmen, denn jeder gezogene
Strich wählt das zuletzt gelegte Teil aus.

**Fahrverhalten**

| Einstellung | Was sie tut |
|---|---|
| **Max. Geschwindigkeit** (0–100 %) | Höchsttempo über dieses Teil. Färbt gleichzeitig das Gleis auf der Fläche. |
| **Fahrtrichtung** — `Vorwärts` / `Rückwärts` | Die Richtung, in der dieses Teil durchfahren wird. |
| **Geschwindigkeitsprofil f(x)** | Zwei Auswahlen untereinander, **Beschleunigung** und **Bremsen**: linear, EaseIn, EaseOut, SCurve, Exponential, Custom. Die Kurve darunter zeigt das Ergebnis, der Text darunter die Formel. `Custom` trägt absichtlich keine feste Formel. |

**Weiche** *(nur bei Weichen-Teilen)*

| Einstellung | Was sie tut |
|---|---|
| **Gestellt auf** — `Stammgleis` / `Zweig` | Die Stellung, die diese Weiche im Plan einnimmt. |

Dort steht auch der Satz, der die Grenze benennt: *„Welcher Motor diese Weiche stellt und wo ihr
Sensor hängt, trägt der Streckenplan schon mit und wandert beim Export mit. Einstellbar wird es
hier, wenn die Ansteuerung kommt."* **Wohin** der Zweig führt, ist keine Einstellung, sondern
Geometrie: ob „Weiche links" oder „Weiche rechts" liegt, und was an ihrem Zweig-Port steckt.

**Sensor**

| Einstellung | Was sie tut |
|---|---|
| **Typ** — `Kein` / `Farbe` / `Abstand` | Welcher Sensor an diesem Teil sitzt. |
| **Aktion beim Auslösen** | Was passiert, wenn er auslöst. |
| **Ziel-Tempo** (%) | Nur sichtbar, wenn die Aktion ein Abbremsen ist: worauf heruntergefahren wird. |

Unten schließt **Teil löschen** den Block ab — dasselbe wie in der Werkzeugleiste, nur in
Reichweite der Hand, die gerade im Inspector ist.

### Die Teileliste

Kopf: `n Teile · n offene Enden`. Darunter jedes gelegte Teil mit Tempo-Farbfleck, Name, Typ und
Löschknopf. Antippen wählt es aus und füllt den Inspector.

**Keine Sortierung, kein Verschieben, kein Duplizieren.** In einem Graphen hat ein Teil Ports,
keine Position in einer Kette — die Datei sagt das so: *„in a graph a part has no position in a
chain, so there is nothing to reorder — this is a parts list, not a sequence."*

### Layout in Textform

```
┌──────────────────────────────────────────────────────────────────────┐
│ ← │ 🚆 │ Streckenplaner       12 Teile 2 Sensoren 3 offen │Speichern│
├──────────────────────────────┬──────────────┬────────────────────────┤
│                              │ n Teile      │ Fahrverhalten      [×] │
│   Zeichenfläche              │ n offen      │  Max. Geschw. ───○──   │
│   • Andockpunkte             ├──────────────┤  Fahrtrichtung         │
│   • Gleise, nach Tempo       │ ■ Gerade 1   │   [Vorwärts][Rückw.]   │
│     gefärbt, mit Tempo-Zahl  │ ■ Kurve r. 1 │  Profil f(x)  ╱▔▔╲     │
│   • gestrichelte Vorschau    │ ■ Weiche 1   │ Weiche                 │
│   • F / A — Sensormarken     │ ■ Bahnhof 1  │  [Stammgleis][Zweig]   │
│                              │              │ Sensor                 │
│  ┌ Tempo ─────┐    [+][-][⤢] │              │  [Kein][Farbe][Abst.]  │
├──────────────────────────────┴──────────────┴────────────────────────┤
│ [Rückgängig][↷][Kreis schließen][Teil löschen]                       │
│ Am gewählten Punkt anhängen                                          │
│ [Gerade][Kurve l.][Kurve r.][Bahnhof][Weiche l.][Weiche r.][Kreuzung]│
│ [Oval][Strecke][Leeren]                                              │
└──────────────────────────────────────────────────────────────────────┘
```

Die Trennlinien zwischen Fläche, Liste und Inspector sind **ziehbar** (240–420 px für die Liste,
320–560 px für den Inspector).

### Auf dem Handy

Unter 720 px Breite — Handy, oder ein schmal gezogenes Fenster — schaltet die Seite um. Die
Zeichenfläche behält die **ganze** Fläche, und beide Seitenbereiche legen sich als Laschen darüber:

| | Breit | Schmal |
|---|---|---|
| **Teileliste** | Spalte rechts der Fläche | Lasche von unten, über den Knopf mit der Teilezahl im Kopf |
| **Inspector** | Eigene Spalte | Lasche von unten, solange ein Teil gewählt ist; der ×-Knopf schließt sie |
| **Knöpfe erste Reihe** | Icon **und** Wort | nur Icon (~190 statt ~390 px) — die Beschriftung steht im Tooltip |
| **Palette** | alle sieben sichtbar | dieselbe Leiste, seitlich gewischt |
| **Kopf** | Symbol, Untertitel, drei Zähler | nur Titel (kürzt sich bei Bedarf) und Speichern, dazu der Teile-Knopf |
| **Tempo-Legende** | unten links | ausgeblendet — Gleisfarbe und Zahl sagen dasselbe |

**Immer nur eine Lasche**: wählst du ein Teil, gewinnt der Inspector und die Teileliste tritt
zurück. Die Teileliste startet geschlossen — erst bauen, dann prüfen.

Gestapelt nahm die Teileliste früher zwei von fünf Zeilen; nach Kopf und Werkzeugleiste blieben für
die Zeichenfläche noch etwa 240 px. Deshalb ist sie jetzt eine Lasche.

---

## Train-Konfiguration

![Train-Konfiguration](images/train-konfiguration.png)

*Erwartet: `docs/images/train-konfiguration.png` — Zugliste links, Zug-Editor rechts.*

Kopfzeile: **„Train-Konfiguration"** mit dem Untertitel **„Powered Up · Legoino-Schnittstelle"**
und der Zählung `n aktiv · n gesamt`.

| Bereich | Wofür |
|---|---|
| **Zugliste** | Alle Züge; „Zurück zur Liste" führt im schmalen Layout dorthin zurück. Das Suchfeld filtert nach Name und Hub-Typ. Ist die Liste leer, sagt sie warum: ohne Trains nennt sie die zwei Wege zum ersten („Hub suchen" und „Neuer Train"), bei einer Suche ohne Treffer „Kein Train passt zu …". |
| **Zug-Editor** | Name, Hub-Modell, BLE-Adresse, Ports, Tempo, Farbe, Beschleunigungs- und Bremsfunktion, aktiv ja/nein. Aktiv-Schalter, Duplizieren und Löschen stehen ab 960 px Fensterbreite neben dem Namen, darunter in einer eigenen Zeile unter ihm. |
| **Hub suchen** | Scannt nach Hubs. Gefundene werden **zur Bestätigung** angeboten, nicht still übernommen; kennt ein Zug die Adresse schon, bindet sich der Hub an ihn statt einen zweiten Zug anzulegen. |
| **Verbinden / Trennen** | Baut die BLE-Verbindung auf — über die Geräte-ID aus der Suche, sonst über die eingetippte BLE-Adresse. |

![Verbindungspanel](images/verbindung-panel.png)

*Erwartet: `docs/images/verbindung-panel.png` — das Panel offen, Servermodus gewählt.*

Der Knopf **„Verbindung (Direkt/Server)"** öffnet das Panel **„Verbindung"**. Dort steht der
Unterschied im Klartext: *„Servermodus: Steuerung über ein Trackify-Backend (Raspberry Pi).
Aus = Direktmodus über das Bluetooth dieses Geräts."* Das Adressfeld zeigt als Beispiel
`http://192.168.1.50:5000`. Das Umschalten wirkt **sofort**, ohne Neustart.

---

## Screenshots aufnehmen

Fünf Bilder, nach `docs/images/`:

| Datei | Inhalt |
|---|---|
| `streckenplaner-uebersicht.png` | Breites Layout, Oval geladen, ein Teil gewählt |
| `streckenplaner-werkzeugleiste.png` | Nur die drei Knopfreihen |
| `streckenplaner-inspector.png` | Inspector mit gesetztem Sensor, Weichen-Block sichtbar |
| `train-konfiguration.png` | Zugliste und Zug-Editor |
| `verbindung-panel.png` | Verbindungspanel offen, Servermodus |

Desktop-Head starten:

```bash
dotnet run --project Source/Trackify/Trackify.csproj -f net10.0-desktop
```

Aufnehmen mit dem Snipping Tool (`Win`+`Shift`+`S`).

Für das schmale Layout — Liste *oder* Editor, Laschen statt Spalten — das Desktop-Fenster
schmal ziehen, oder die APK aus `android-apk.yml` nehmen und am Gerät aufnehmen. Dort sieht man
außerdem, wie die Systemleisten über `utu:SafeArea` ausgespart werden.
