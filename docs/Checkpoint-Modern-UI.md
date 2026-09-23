# Checkpoint vor der Web-Erweiterung

Stand: 23.09.2026. Dieser Checkpoint enthält die native Desktop-Anwendung und die
bis dahin im Arbeitsverzeichnis vorhandenen Verbesserungen. Der Branch
`modern-ui` soll den fertigen Stand vor der Web-Erweiterung festhalten.

## Oberfläche

`Views/SurfaceHost.cs` schaltet zwischen moderner und klassischer Oberfläche mit
demselben `MainWindowViewModel`. `ModernView.axaml` definiert Rahmen und Styles;
`ModernView.axaml.cs` erzeugt die sechs Seiten Übersicht, Filter, Solar, Wasser,
Zisterne und System. Die Dialognavigation liegt in `ModernView.Dialogs.cs`, die
Sensor-Texteditoren in `ModernView.SensorSettings.cs`.

Die Oberfläche verwendet dieselben Anlagenmodelle wie die klassische Ansicht.
Anzeigeformat und Einheit werden aus dem Sensor gelesen; freie Namen werden
auch in der klassischen Ansicht angezeigt. Bekannte Ressourcenschlüssel werden
weiter übersetzt. MQTT-Topics werden nicht durch einen Anzeigenamen umbenannt.
`UiEditSession` erkennt, wenn der bearbeitete Wert seit Öffnen des Editors durch
einen anderen Zugriff geändert wurde. Dann wird das Überschreiben abgelehnt.

Ein Dialogverlauf bewahrt vorherige Inhalte und Fokus. Schließen, Escape und
Übernehmen führen eine Ebene zurück. Erst die letzte Ebene aktiviert wieder die
Hauptseite. Die Anzeige wird einmal pro Sekunde aktualisiert. UI-Präferenzen
stehen getrennt von Anlagenwerten in `ui.json`; siehe [Bedienung](Modern-UI.md).

## Start und Shutdown

- `App` verwendet dieselbe MQTT-Instanz für Haupt- und untergeordnete Modelle.
- Die Konfiguration wird geladen und die Objekt-/Relaiszuordnung aufgebaut,
  bevor `MainWindowViewModel.OnStarted` validiert, GPIO öffnet und die Modelle startet.
- `ViewModelBase.Start` startet explizit und idempotent; inaktive Modelle
  veröffentlichen keine Zustandsmeldungen. Das ist keine vollständige Trennung
  von UI und Hardware: Messobjekte entstehen weiterhin in Modellkonstruktoren.
- `TimerLifetime` verfolgt aktive und bereits deaktivierte Timer samt laufenden
  Callbacks. Beim Stoppen werden diese abgewartet. Ein gestoppter Lebenszyklus
  wird nicht erneut gestartet; der Oberflächenwechsel verwendet ihn weiter.
- Beim regulären Fensterschließen wartet `ShutdownAsync` auf Messungen/Befehle,
  speichert den Zustand, schaltet Relais aus, schließt GPIO und trennt MQTT.
  Ein erzwungenes Prozessende ersetzt diesen Ablauf nicht.
- `TimeTrigger` verhindert erneutes Aktivieren nach dem Stoppen.

## Messung, Befehle und Konfiguration

- Erfolgreiche Messungen setzen Wert und Zeitstempel vor dem Veröffentlichen.
  Fehler überschreiben den letzten erfolgreichen Wert nicht.
- Distanzmessung: monotone Laufzeitmessung, jeweils 50 ms Echo-Timeout,
  Überlappungsschutz, gültige Stichprobenanzahl und kooperativer Abbruch.
- `Distance.OpenedPins` hält die tatsächlich beim Start geöffneten Pins fest.
  Eine neue Adresse wird gespeichert, aber erst nach Neustart verwendet;
  beim Beenden werden die alten, tatsächlich geöffneten Pins geschlossen.
- `IEzoCommandDevice` stellt Sensorbefehle für die EZO-Modelle bereit. Der
  UI-Abschluss eines Befehls ist kein Nachweis einer erfolgreichen Kalibrierung.
- `PropertySetter` konvertiert invariant, prüft Datentypen und DataAnnotations
  und lehnt unter anderem nicht endliche Zahlen ab.
- `ConfigurationValidator` prüft die konfigurierten Objekte vor Hardwarestart.
  Timerbasierte Messmodelle verlangen Intervalle von 1 bis 86400 Sekunden;
  andere Modelle können 0 verwenden. Uhrzeiten müssen innerhalb eines Tages liegen.
- MQTT-Themen werden über `MqttTopic` einheitlich behandelt (Windows-Präfix
  `win`). `MqttCommandInput` begrenzt Pfade auf 1–3 Segmente, 256 Zeichen
  insgesamt bzw. 64 pro Segment; Nutzdaten auf 1024 Bytes und gültiges UTF-8.
- `Persistence` serialisiert Speicher-/Lesezugriffe. Schreiben erfolgt über
  eine temporäre Datei im Zielverzeichnis, Flush und Ersetzen der Zieldatei.
  Fehler werden protokolliert; die aktuelle API liefert keinen Speichererfolg
  an den UI-Aufrufer zurück.

## Dateien und Betriebsdaten

`appsettings.json` enthält Laufzeit-, MQTT- und Logging-Einstellungen.
`poolcontrolviewmodel.json` ist die Linux-Anlagenkonfiguration im aktuellen
Arbeitsverzeichnis. Auf Windows wird `winpoolcontrolviewmodel.json` verwendet.
Der eingecheckte Windows-Stand enthält lokale Mess-/Zeitwerte und ist keine
Kopie der Pi-Konfiguration und kein reproduzierbarer Testdatensatz. Die Tests
verwenden isolierte Fixtures. Laufzeitlogs und `artifacts/` werden ignoriert.
Die bisher getrackten Dateien `log20260531.txt` und `log20260531_001.txt`
werden aus dem Git-Index entfernt und lokal beibehalten. Frühere Commits werden
nicht umgeschrieben; dort bleiben die damaligen Logs Teil der Historie.

## Verifizierter Betrieb und Grenzen

Die ARM64-Version wurde auf `pi@192.168.39.177` unter `/home/pi/PoolControl`
installiert, mit geöffnetem Fenster gestartet und mit MQTT verbunden. Die
Konfiguration wurde beim Erstdeployment aus `/home/pi/poolcontrolviewmodel.json`
übernommen; spätere Updates behielten die deployte Datei bei. Das ungültige
Distanzintervall 0 wurde auf ausdrücklichen Wunsch auf 60 Sekunden gesetzt.

Beim ersten vollständigen Messzyklus meldeten die konfigurierten
Temperatursensoren fehlende 1-Wire-Dateien und der Distanzsensor einen
Echo-Timeout. pH/Redox lieferten Antworten; daraus folgt keine Aussage zur
physikalischen Plausibilität oder Kalibrierung. Diese Hardwareprobleme wurden
nicht durch die UI-Arbeiten behoben. Die Automatik muss anhand tatsächlicher
Sensoren und Verkabelung geprüft werden.

Relaisanzeigen stellen den Steuerzustand dar, keine elektrische Rückmeldung.
Das Layout wurde automatisiert bei 1024 × 600 geprüft; auf dem Pi wurde ein
1024 × 576 großes Fenster beobachtet. Vollständige Touch-/Hardwareabnahme ist
damit nicht ersetzt. Für diesen Stand wurde kein dauerhafter Autostart eingerichtet.

Builds haben bestehende Nullable-Warnungen. Zeitweise war die NuGet-Abfrage
nach Sicherheitsinformationen nicht erreichbar (`NU1900`); das ist kein
erfolgreicher Abhängigkeits-Sicherheitsaudit. Die aktuellen Testbefehle stehen
in [Tests/README.md](../Tests/README.md).

## Nächster Schritt

Die Browseroberfläche ist noch nicht implementiert. Sie erfordert die Trennung
von Anzeige und Hardwarezugriffen sowie eine zentrale API auf dem Pi. Siehe
[Web-Architektur](Web-Architektur.md). Dieser Checkpoint enthält keinen Webserver,
keine Browserauthentifizierung und kein Browser-Deployment.
