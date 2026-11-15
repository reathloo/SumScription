# payToCalc — UI-Vertrag
Avalonia 11 · Desktop (macOS/Linux/Windows) · alle sichtbaren Texte deutsch · Dark + Light · nur Vektor · Mindestgröße 360 × 560 · frei skalierbar.
## 1 Idee
payToCalc sieht aus wie ein Labor-Messgerät mit Papiervorschub, nicht wie ein Formular: Das Rechenband ist ein perforierter Papierstreifen aus einem Metallschlitz, auf den sich das Ergebnis „druckt".
Unverwechselbar im Screenshot: helle Streifenfläche mit Lochreihe an beiden Kanten, zwei Fadenkreuze an den Ecken, flaches Blech statt Karten — keine Schlagschatten, kein Radius über 4 px, kein Fluent-Standardlook (alle Werte stammen aus eigenen ControlThemes).
## 2 Palette
Kontrast = WCAG-Relativluminanz, eine Nachkommastelle; „auf Y" nennt die gemessene Fläche. Papierwerte sind in beiden Themes identisch (physisches Medium); im Light-Theme grenzt die 2-px-Papierkante (14.49:1) das Band ab, nicht die Füllung (1.10:1).

| Rolle | Dark | Light | Kontrast (Dark / Light) |
|---|---|---|---|
| Fläche (Chassis) · erhöht / vertieft (Tasten / Funktionstasten) | #141A1F · #232C34 / #171E24 | #E1E6EB · #F4F7F9 / #D3DAE0 | Bezugsfläche; erhöht 1.24:1 / 1.18:1, vertieft 1.04:1 / 1.13:1 zu Fläche |
| Text primär | #E8EDF2 | #10151A | 12.03:1 / 17.06:1 auf erhöht |
| Text sekundär | #9FAAB8 | #4E5966 | 7.15:1 / 5.05:1 auf vertieft |
| Akzent / Akzent-Text | #35D6BA / #04211C | #0E7A6E / #FFFFFF | 9.59:1 / 4.15:1 auf Fläche; Text auf Akzent 9.26:1 / 5.21:1 |
| Fokusring | #62C8FF | #0A6C9E | 9.00:1 / 4.07:1 auf vertieft |
| Gefahr/Fehler · Messing (Schloss) | #A8261C · #8A6414 | #A8261C · #8A6414 | 6.20:1 / 4.68:1 auf Papier |
| Papier (Band, Preisblatt) · Kante 2 px | #EDF0F1 · #6E7C8A | #EDF0F1 · #11161A | Füllung 15.31:1 / 1.10:1; Kante 4.10:1 / 14.49:1 zu Fläche |
| Papier-Tinte / Papier-Tinte schwach | #11161A / #4A555F | #11161A / #4A555F | 15.90:1 / 6.65:1 auf Papier |
| Paywall-Schleier | #05080B @ 90 % | #0B1016 @ 68 % | dimmt das Papier 14.45:1 / 6.28:1 herunter |

## 3 Typografie
Schriften ohne neue NuGet-Abhängigkeit: Sans „SF Pro Text, Segoe UI Variable Text, Segoe UI, Roboto, Noto Sans, Ubuntu Sans, Cantarell, system-ui, sans-serif"; Mono „SF Mono, Menlo, Cascadia Mono, Consolas, Ubuntu Mono, DejaVu Sans Mono, monospace". Mono für alle Ziffern (Spalten stehen fest), Sans für Beschriftung, Chip und Preisblatt (Überschrift 17 px/600/−0.01 em).

| Rolle | Familie | Größe | Gewicht | Laufweite | Zeilenhöhe |
|---|---|---|---|---|---|
| Ergebnis (Band) | Mono | 30 px | 600 | −0.01 em | 34 px |
| Ausdruck | Mono | 14 px | 500 | 0 | 20 px |
| Taste | Mono | 17 px | 600 | 0 | 20 px |
| Verlaufseintrag | Mono | 12 px | 500 | 0 | 18 px |
| Meta, Chip, Preisblatt | Sans | 11 px | 500 | +0.02 em | 16 px |

## 4 Raster und Form
- Basisraster 4 px, Abstandsschritte 4 / 8 / 12 / 16 / 24 px; Kopfleiste 36 px, Fußleiste 44 px, Tastenhöhe 46 px, Tastenfuge 4 px, Tastenbreite = (B − 40) / 5 = 64 px bei 360 px Breite.
- Radien: Papier 0, Taste 4 px, Preisblatt 2 px, Chip 3 px, Fokusring 2 px; keine Schlagschatten, keine Verläufe, Tiefe nur über 1-px-Kanten (Tastenkante #3A4550 / #C7CFD6, rein dekorativ — Bedienbarkeit hängt an Text ≥ 4.5:1 und Fokusring ≥ 3:1). Prägend ist der Streifen: Papierfläche mit 2-px-Kante, Lochreihe beidseitig (Loch 5 × 7 px, Radius 3 px, Teilung 16 px, 10 px vom Rand, Aussparung in Chassis-Farbe), Fadenkreuze an den vier Ecken der Live-Zone (12 px Arme, 1 px, dekorativ) — alles Vektorpfade, DPI-fest skaliert.
## 5 Motion

| Interaktion | Dauer | Kurve |
|---|---|---|
| Tastendruck / -loslassen | 80 ms / 140 ms | ease-out, Flächenwechsel + 1 px Absenkung |
| Ergebnis erscheint | 160 ms | ease-out, Opazität 0 → 1, Versatz 4 px → 0 |
| Fehler | 140 ms / 160 ms | Opazität 0 → 1 / zweimal 3 px seitlich |
| Ergebnis verdeckt | 200 ms | ease-out, Unschärfe 0 → 6 px, Opazität → 0.35 |
| Freischalten + Tarif-LED | 200 ms | ease-out, Unschärfe 6 → 0 px, Leuchtkraft 0 → 1 |
| Preisblatt, Schleier, Dialog | 180 ms | ease-out, Blatt 12 px → 0 px bzw. Opazität + Skalierung 0.98 → 1; Schließen je 140 ms ease-in |
| Neuer Verlaufseintrag | 140 ms | ease-out, Versatz −6 px → 0 |

Keine Dauer-Animationen, kein Flackern beim Start. Reduzierte Bewegung: alle Zeiten 1 ms, nur Opazitätswechsel — kein Verschieben, Skalieren, Rütteln oder Unschärfe-Übergang.
## 6 Layout bei 360 × 560
```
    ○  12 + 30                ○   ← Live 92: Ausdruck 14 px
    ○                42        ○      Ergebnis 30 px, rechtsbündig
    ○ - - perforation - - - - ○ ← Journal 134, 20 Zeilen, 12 px Mono
254 AC  C   ⌫   %   ÷            5×5, Fuge 4 px, Taste 64×46 px
    7   8   9   ±   ×            Z4: 1 2 3 [=]   Z5: 0 0 , [=]
    4   5   6   +   −            „=" 2×2 (Zeilen 4–5, Spalten 4–5)
    1   2   3  [=][=]
    0     ,  [=][=]
```
Kopfleiste 36 px mit „payToCalc" und Tarifchip, Papier 226 px, Fußleiste 44 px mit „Demo zurücksetzen" und „Ergebnis freischalten". Breite: Papier und Leisten volle Breite, das Tastenraster bricht nie um, Texte kürzen per Auslassung. Höhe: Journal flexibel (min 108 px, max 360 px), Tastenhöhe 46 px fix und ab 720 px Fensterhöhe 52 px; Schriftgrößen folgen nur der System-DPI.
## 7 Zustände
- **Normal:** Ausdruck und Ergebnis stehen rechtsbündig im Papier, das Journal listet die letzten Auswertungen, neueste oben, die fünf ältesten auf 45 % Deckkraft.
- **Fehler:** Die Ergebniszeile zeigt „Division durch null" oder „Zahl zu groß" in #A8261C (6.20:1 auf Papier); Ausdruck und Journal bleiben unverändert lesbar.
- **Ergebnis verdeckt:** Die Ergebniszeile ist mit Unschärfe 6 px und 35 % Deckkraft maskiert, darüber liegt das Messing-Schloss auf einem 36 × 40 px Papierchip; Ausdruck und Journal bleiben lesbar.
- **Paywall-Overlay:** Der Schleier dimmt das ganze Fenster, mittig liegt das Papier-Preisblatt (320 px breit) mit „Ergebnis gesperrt", „Demo — es wird nichts berechnet.", zwei Preiszeilen (Abo 1,99 €/Monat, jederzeit kündbar / Einmalzahlung 4,99 €) und „Schließen"; danach erscheint in der Fußzeile der Knopf „Ergebnis freischalten", der das Blatt erneut öffnet.
- **Checkout-Dialog:** Auf dem Chassis liegt ein Blechpanel (Fläche erhöht) mit „Kaufübersicht", zwei Zusammenfassungszeilen, der Hinweiszeile „Demo — es wird nichts berechnet." und den Knöpfen „Demo-Kauf bestätigen" / „Abbrechen".
- **Freigeschaltet:** Die Ergebniszeile entschärft in 200 ms, der Kopf-Chip wechselt auf „Abo aktiv (Demo)" bzw. „Einmalkauf (Demo)", die LED im Chip leuchtet 200 ms in Akzent auf.
## 8 Barrierefreiheit
- Fokusring und Mindestkontraste: 2 px Fokusfarbe plus 1 px Innenring #11161A, sofort sichtbar und nie ausgeblendet; Fokusring ≥ 3:1 gegen jede Nachbarfläche (9.00:1 Dark / 4.07:1 Light auf vertiefter Fläche), alle Textpaarungen ≥ 4.5:1 (kleinster Wert 5.05:1), Grafik und große Elemente ≥ 3:1 (kleinster Wert 4.07:1); Schriftgrößen folgen der System-DPI und werden nicht geklemmt.
- Fokusreihenfolge und Tastatur: Kopfzeile → Tastenfeld zeilenweise links → rechts ohne Wraparound → Fußzeile; Overlay und Dialog setzen den Fokus in den Dialog, ESC schließt, danach kehrt der Fokus zum auslösenden Knopf zurück. Ziffern und Operatoren tippen direkt, Backspace löscht, Enter löst „=" aus, Pfeiltasten bewegen zweidimensional im Tastenfeld; jedes Element trägt einen deutschen Namen („Sieben", „Geteilt", „Ergebnis").
- Reduzierte Bewegung: Zeiten auf 1 ms, nur Opazität, kein Verschieben, Skalieren, Rütteln oder Unschärfe-Übergang; Start ohne Flackern. Touch-Ziele ≥ 44 px Höhe, Tastenraster 64 × 46 px.
