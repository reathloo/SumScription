# payToCalc

Ein moderner Desktop-Taschenrechner mit halbtransparentem „Liquid Glass“-Look und einer absichtlich übertriebenen, vollständig fiktiven Paywall.

> [!IMPORTANT]
> Die Paywall ist eine Parodie. Es werden keine Zahlungsdaten abgefragt, keine Käufe ausgeführt und keine Gebühren berechnet.

## Funktionen

- Grundrechenarten, Prozent, Vorzeichenwechsel und Dezimalzahlen
- Punkt-vor-Strich-Auswertung und lesbarer Rechenverlauf
- Vollständige Bedienung über Tastatur und Ziffernblock
- Dark- und Light-Theme mit skalierbarer Avalonia-Oberfläche
- Reduzierte Bewegung über `PAYTOCALC_REDUCED_MOTION=1`
- Acht sichtbare kostenlose Auswertungen; bei der neunten erscheint die Paywall-Parodie
- Drei bewusst nervige Ablehnungsstufen, bevor die Eingabe wieder freigegeben wird
- Lokaler Demo-Zustand ohne Benutzerkonto oder Netzwerkzugriff

## Voraussetzungen

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- macOS, Linux oder Windows

## Starten

```bash
git clone https://github.com/reathloo/payToCalc.git
cd payToCalc
dotnet run --project src/PayToCalc.App/PayToCalc.App.csproj
```

Die Debug-Konfiguration zeigt gegebenenfalls den Avalonia-Diagnostics-Schalter. Für eine Oberfläche ohne Debug-Werkzeuge:

```bash
dotnet run --configuration Release --project src/PayToCalc.App/PayToCalc.App.csproj
```

## Entwickeln und prüfen

```bash
dotnet restore PayToCalc.slnx
dotnet build PayToCalc.slnx --configuration Release --no-restore
dotnet test PayToCalc.slnx --configuration Release --no-build
```

## Projektstruktur

```text
src/PayToCalc.App/       Avalonia-Oberfläche und ViewModel
src/PayToCalc.Core/      Rechner-, Verlaufs- und Paywall-Logik
tests/                   xUnit-Tests
docs/design.md           Aktuelle Designrichtung
```

Der Paywall-Zähler wird als kleine JSON-Datei im plattformspezifischen Anwendungsdatenverzeichnis gespeichert. Der Rechenverlauf bleibt ausschließlich im Arbeitsspeicher und wird beim Beenden verworfen.
