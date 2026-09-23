# Testübersicht

Die Prüfungen sind ausführbare Konsolenprogramme mit Assertions, keine
klassischen Test-Runner-Projekte. `dotnet test` allein führt ihre Assertions
nicht zuverlässig aus. Fehler müssen am Exitcode erkannt werden.

## Ergebnis des Checkpoints vom 23.09.2026

Alle neun Programme wurden über `Run-All.ps1` erfolgreich gebaut und ausgeführt.
Die UI-Prüfung umfasste 36 Seiten-/Sprach-/Theme-Kombinationen. Einige Builds
meldeten `NU1900`, weil die NuGet-Abfrage nach Sicherheitsinformationen nicht
erreichbar war; diese Prüfung konnte damit keine vollständige Aussage über
bekannte Paket-Sicherheitsprobleme treffen.

## Ausführung

Voraussetzungen: .NET-10-SDK, für `EzoCommand` zusätzlich .NET-8-Runtime,
wiederherstellbare NuGet-Pakete; die UI-Prüfung wird auf Windows mit
Avalonia.Headless und Skia durchgeführt.

Alle neun Programme isoliert bauen und ausführen:

```powershell
./Tests/Run-All.ps1
```

Das Skript verwendet pro Programm ein eigenes Verzeichnis unter
`artifacts/checks/`, prüft die Exitcodes und bricht beim ersten Fehler ab.
Produktive Konfigurationen werden nicht als Testdaten verwendet. Der UI-Test
schreibt eigene Einstellungen und Fixtures; der Start-Lebenszyklustest erhält
eine isolierte Logging-Konfiguration ohne produktive Ziele. Kein Test bedient
echte Relais oder Sensoren.

| Programm | Prüfgegenstand |
| --- | --- |
| DistanceMeasurement | Echo-Timeouts, Überlappung, Mittelung, Fehlererholung, Abbruch, bereits geöffnete Pins |
| EzoCommand | Weitergabe von Sensorbefehlen an das EZO-Geräteinterface |
| MeasurementOrder | Wert/Zeitstempel vor Veröffentlichung; keine Veröffentlichung bei Messfehlern |
| MqttTopic | Linux-/Windows-Präfixe und Normalisierung von Command-Topics |
| Persistence | Paralleles Speichern/Laden, Sperren und Fehlerpfade |
| PropertySetter | Root-/Objekt-/Dictionary-Zugriffe, Konvertierung und ungültige Eingaben |
| Shutdown | Warten auf Callbacks, idempotentes Stoppen, deaktivierte Timer, Trigger-Neustarts |
| StartupLifecycle | Expliziter Start, Startvalidierung, keine Publikation inaktiver/gestoppter Modelle |
| Ui | Präferenzen, 36 Seiten-/Sprach-/Theme-Kombinationen, Editoren, Navigation, Sensorfelder und Oberflächenwechsel |

Die UI-Prüfung deckt zusätzlich ab:

- deutsches Zahlenformat, Wertebereiche, Konflikte mit externen Änderungen;
- Solar- und Lampenstatus, `PoolLight`-/`Poollampe`-Konfiguration;
- Sensoradresse und Textfelder, Vorschau, Formatfehler und extreme Präzision;
- Speicherung und erneutes Laden von Namen, Einheit und Zahlenformaten;
- invariant formatierten Wert für die bestehende MQTT-Publikationsfunktion
  (kein End-to-End-Test gegen einen echten Broker);
- Messalter in Jahren/Tagen/Stunden/Minuten/Sekunden, Schaltjahr und Zukunft;
- Zurücknavigation nach Schließen, Übernehmen und Escape;
- wiederholten Modern-/Classic-Wechsel mit demselben Anlagenmodell.

Renderbilder liegen unter `artifacts/checks/Ui/ui-test-data/`. Vor dem Checkpoint
wurden unter anderem Übersicht, Adresseditor und Schnittstellenformat-Editor
visuell geprüft. Automatische Tests ersetzen keine Prüfung des echten
Pi-Touchscreens, der Verkabelung, Sensorplausibilität oder Kalibrierung.

Einzelne Testverzeichnisse enthalten zusätzliche READMEs. Bei direkter Ausführung
des UI-Tests ist dessen separates Ausgabeverzeichnis zwingend einzuhalten, da er
`appsettings.json` neben seiner eigenen Assembly schreibt.
