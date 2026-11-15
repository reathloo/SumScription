# SumScription — Designrichtung

## Charakter

SumScription ist ein moderner Desktop-Taschenrechner mit einem ruhigen, halbtransparenten „Liquid Glass“-Look. Die Oberfläche bleibt während des Rechnens bewusst seriös; erst die fiktive Paywall bricht diesen Ton als übertriebener Gag.

## Oberfläche

- Tiefblauer, halbtransparenter Fensterhintergrund mit zwei unscharfen Farbkörpern statt dekorativer Kartenlandschaft.
- Eine große Glasfläche bündelt Ausdruck, Ergebnis und Verlauf.
- Vier vertraute Tastenspalten; Operatoren bilden eine durchgehende orange Achse.
- SF Pro beziehungsweise geeignete Systemschriften; große, leichte Ergebnisziffern und kompakte Beschriftungen.
- Dark und Light Theme nutzen dieselbe Hierarchie mit angepassten semantischen Farben.
- Sichtbare Hover-, Press- und Tastaturfokus-Zustände; Tastatur und Ziffernblock funktionieren vollständig.
- Reduzierte Bewegung wird über `SUMSCRIPTION_REDUCED_MOTION=1` unterstützt.

## Paywall-Parodie

- Die Paywall erscheint nicht beim Start, sondern erst nach acht sichtbaren erfolgreichen Auswertungen. Die neunte Antwort wird verdeckt.
- Fehler und leere Auswertungen verbrauchen kein Freikontingent.
- Das Overlay nennt gut sichtbar „100 % fiktiv · 0 % echter Kauf“ und erfasst keine Zahlungsdaten.
- Wer nicht „kauft“, muss drei unterschiedliche Ablehnungsstufen bestätigen. Erst danach wird die Rechnereingabe wieder freigegeben.
- Der Demo-Kauf führt nur zum bereits lokalen Freischaltzustand. Es gibt in der Oberfläche bewusst keinen Zurücksetzen-Knopf und keinen Statuschip, der die Überraschung vorwegnimmt.
- Die absichtlich absurden Fantasiepreise liegen bei 29,99 € pro Woche beziehungsweise 499,99 € einmalig.
- Magenta, Violett, Orange, Cyan und Warn-Gelb machen das Overlay deutlich lauter als den eigentlichen Rechner; Deal-Badges und ein Warnband überzeichnen typische Upsell-Muster.

## Größen und Bewegung

- Standardfenster: 420 × 700, Mindestgröße 390 × 650, frei skalierbar.
- Tasten: 60 Punkte hoch, ab 760 Punkten Fensterhöhe 66 Punkte.
- Glasflächen verwenden große Radien; kleine Bedienelemente deutlich kleinere Radien.
- Eingabe reagiert sofort. Ergebnis und Overlays bewegen sich kurz und zurückhaltend; nur die absichtlich nervige Ablehnungsstufe erhält eine kurze, einmalige Rüttelreaktion.
