# Geplante Avalonia-Weboberfläche

Status am 23.09.2026: Konzept, noch nicht implementiert. Zunächst wird der
Desktop-Stand dokumentiert und als Branch `modern-ui` festgehalten. In dieser
Phase werden keine Browserprojekte hinzugefügt und keine Laufzeitdienste geändert.

## Ziel

Dieselbe moderne Bedienoberfläche soll lokal auf dem Raspberry Pi und im
Browser nutzbar sein. Avalonia unterstützt WebAssembly und kann damit C#- und
Avalonia-UI-Code im Browser ausführen:

- [Avalonia WebAssembly](https://docs.avaloniaui.net/docs/platform-specific-guides/webassembly)
- [Browser-Deployment](https://docs.avaloniaui.net/docs/deployment/webassembly)

Die Browserausgabe besteht aus statischen Dateien einschließlich .NET-Laufzeit.
Für die Steuerung der realen Anlage braucht PoolControl zusätzlich einen Server
auf dem Pi. Der initiale WebAssembly-Download ist größer als eine kleine HTML-Seite.

## Vorgeschlagene Aufteilung innerhalb derselben Solution

| Teil | Verantwortung |
| --- | --- |
| Gemeinsame UI | ModernView, Styles, Dialoge, reine Anzeige-Modelle, Formatierung |
| Steuerung | Messungen, GPIO/I²C/1-Wire, Automatik, MQTT, Persistenz |
| Pi-Host/API | Zentraler Anlagenzustand, geprüfte Befehle, Webdateien, Live-Daten |
| Desktop-Einstieg | Lokales Fenster und Verbindung zur Steuerung |
| Browser-Einstieg | Avalonia WebAssembly, Zustandsabgleich und Befehle über API |

Projektnamen und genauer Transport sind noch festzulegen. HTTP plus SignalR
oder WebSocket sind mögliche Bausteine, keine bereits getroffene Implementierungsentscheidung.

## Zu erhaltende Eigenschaften

1. Genau eine Steuerungsinstanz besitzt die Hardware. Weitere Browser starten
   keine Regelungstimer und keine MQTT-/GPIO-Treiber.
2. Die Automatik läuft ohne offenen Browser weiter.
3. Anlagenwerte werden zentral auf dem Pi geprüft und gespeichert. Ein Browser
   speichert keine unabhängige, konkurrierende Anlagenkonfiguration.
4. Änderungen und Messungen erscheinen auf Desktop und Browser. Gleichzeitige
   Eingaben brauchen serverseitige Versions-/Konfliktprüfung; der heutige lokale
   `UiEditSession`-Vergleich allein reicht dafür nicht aus.
5. Zugangskontrolle, Verbindungsabbruch, Wiederverbindung und der Umgang mit
   veralteten Daten werden vor Freigabe schreibender Browserfunktionen umgesetzt.
6. Neue Versionen behalten Adressen, Formate, Relaiszuordnung und bestehende
   MQTT-Topics bei. Einheitenbeschriftungen führen keine implizite Umrechnung ein.

## Bekannte Umbaupunkte

`ModernView` ist bereits ein wiederverwendbares Control, greift aber direkt auf
`MainWindowViewModel`, Sensorbefehle und `Persistence.Instance` zu. Modellkonstruktoren
erzeugen Messobjekte. `App` startet bisher einen Desktop-Lebenszyklus mit Window.
Diese Verbindungen müssen über Dienste und reine Anzeige-/Transportmodelle
getrennt werden. Dateispeicherung, Beenden der Anwendung, Desktopfenster und
UI-Präferenzen benötigen plattformspezifische Implementierungen.

Vor dem Umbau ist ein kleiner Browser-Build mit den tatsächlich verwendeten
Avalonia-Paketen zu prüfen (einschließlich Styles, ReactiveUI und Trimming).
Danach zuerst eine lesende Übersicht gegen den Pi, anschließend schreibende
Einstellungen mit Validierung, Persistenz und Konflikttests. Das ist ein
Vorschlag für die nächste Arbeitsphase, keine Freigabe oder fertige Funktion.
