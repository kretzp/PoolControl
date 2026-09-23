# Moderne Avalonia-Oberfläche

Die Anwendung startet ohne gespeicherte Auswahl mit der modernen Ansicht bei 1024 × 600. Sie ist nativ in Avalonia umgesetzt; es wird kein Browser eingebettet.

- **Modern → Klassisch:** oben rechts „Klassisch“ oder System → „Klassische Oberfläche“.
- **Klassisch → Modern:** Schaltfläche „Modern“ links unten, unterhalb der bisherigen Navigation.
- **Sprache:** oben „Deutsch“ / „English“ antippen. Die moderne Oberfläche schaltet direkt um; die klassische Oberfläche behält ihre bisherige Lokalisierung.
- **Theme:** oben zwischen System, Hell und Dunkel wechseln. Die klassische Ansicht behält ihr bisheriges dunkles Design.
- **Einstellungen:** Zahl antippen, mit großen Plus-/Minus-Tasten oder Touch-Ziffernfeld bearbeiten, „Übernehmen“. Bei extern geänderten Werten wird ein Überschreiben abgelehnt; Dialog erneut öffnen.
- **Zeiten:** Antippen öffnet einen Stunden-/Minuteneditor mit großen Plus-/Minus-Tasten.
- **Sensoren:** Wasser → „Sensor & Kalibrierung“ oder System → „Alle Sensoren“. Vorhandene Kalibrierbefehle sind dort mit Referenzwert und Bestätigung verfügbar.
- **Dialognavigation:** „Schließen“ und Escape gehen jeweils eine Ebene zurück. Nach Sensordetails oder einer Relaisaktion erscheint wieder die vorherige Auswahlliste mit ihrer Scrollposition. Erst das Schließen der Liste führt zurück zum Reiter.
- **Sensoradresse:** In den Sensordetails die Adresse antippen und über Tastatur oder Touch-Tasten bearbeiten. Temperaturadressen sind 1-Wire-IDs, pH/Redox verwenden dezimale I²C-Adressen. Änderungen werden mit „Übernehmen“ gespeichert; externe Änderungen werden nicht überschrieben. Neue Trigger-/Echo-GPIOs der Zisterne gelten nach einem Anwendungsneustart, bis dahin bleiben die bisherigen Pins aktiv.
- **Sensorbeschriftung und Formate:** Name, Maßeinheit, Format UI und Schnittstellenformat sind in den Sensordetails mit Text-/Touch-Tastatur editierbar. „Übernehmen“ speichert die Werte sofort. Freie Namen werden in beiden Oberflächen angezeigt; bekannte Übersetzungsschlüssel bleiben lokalisiert. Das UI-Format verwendet die Anzeigesprache, das Schnittstellenformat gilt ab der nächsten MQTT-Veröffentlichung mit Dezimalpunkt. MQTT-Topics bleiben an den bestehenden Sensorschlüsseln ausgerichtet. Eine andere Maßeinheit ändert die Beschriftung, nicht die physikalische Skalierung. Formatfelder zeigen eine Vorschau, z. B. für `0.00` oder `#0.000`; leer verwendet das Standardformat.
- **Übersicht:** Filter- und Solarpumpe zeigen ihren aktuellen Steuerzustand mit „Ein“/„Aus“ und können nach Bestätigung manuell geschaltet werden.
- **Poollampe:** Die Übersicht verwendet dasselbe Lampenrelais wie „System → Relais / Ausgänge“. Die Konfigurationsschlüssel `PoolLight` und `Poollampe` werden unterstützt.
- **Messalter:** Beispielsweise `2 J 3 T 4 h 5 min 6 s`; Null-Einheiten entfallen. Jahre werden anhand des Kalenderdatums berechnet, einschließlich Schaltjahren. Zukünftige Zeitstempel zeigen `0 s`. Fehlende/ungültige Messungen bleiben als solche gekennzeichnet. Als veraltet gilt ein Wert nach mehr als dem größeren Wert aus fünf Sekunden und drei Messintervallen.

Oberfläche, Sprache und Theme stehen getrennt von den Anlagenwerten in `Environment.SpecialFolder.LocalApplicationData/PoolControl/ui.json` (Windows: `%LOCALAPPDATA%/PoolControl/ui.json`). Bei fehlender oder beschädigter Datei werden Standardwerte verwendet. Anlagenwerte werden weiterhin durch den bestehenden Persistenzmechanismus gespeichert.

Beide Ansichten teilen dasselbe MainWindowViewModel. Beim Umschalten werden Hardware, MQTT-Verbindung und Regelungstimer nicht neu gestartet. Die moderne Anzeige aktualisiert ihre Messwerte einmal pro Sekunde; ihr Anzeigetimer läuft nur, solange die Ansicht eingeblendet ist. Eigene Formatkultur für Deutsch/Englisch verändert keine Prozesskultur und damit keine MQTT-Datenformate.

Relaisanzeigen zeigen den vorhandenen **Steuerzustand**, keine elektrische Rückmeldung. Manuelle Änderungen können durch die bestehende Automatik wieder geändert werden. Eine zeitlich begrenzte Hand-/Auto-Übersteuerung ist nicht Bestandteil der vorhandenen Steuerung und wird daher nicht vorgetäuscht. Ebenso gibt es keinen erfundenen MQTT-Verbindungsstatus und keine Prozentanzeige der Zisterne ohne bekannte Kapazität.

Kalibrierung nutzt die bestehenden Sensorbefehle. Nach einem Befehl den Kalibrierstatus abfragen; ein beendeter Befehl allein wird nicht als erfolgreiche Kalibrierung ausgewiesen. Umfangreiche Wartungslisten scrollen, die sechs Hauptseiten passen in die Zielgröße. Name, Maßeinheit sowie Anzeige- und Schnittstellenformat sind in der modernen Sensor-Konfiguration verfügbar. Die separaten Volumenfelder der Zisterne (`NameL`, `UnitSignL`, `ViewFormatL`, `InterfaceFormatL`) haben hier keinen eigenen Editor.

## Eingaben und Speicherung

Sensoradressen und Sensor-Textfelder werden beim Übernehmen unmittelbar über `Persistence.Save` gespeichert. Andere Anlagenwerte nutzen die bestehende periodische Speicherung und den geordneten Shutdown. Das Speicherintervall steht in `Settings.PersistenceSaveIntervalInSec` in `appsettings.json`. Die Ausgangsdatei unter `/home/pi` wird durch spätere Änderungen in der Anwendung nicht aktualisiert; maßgeblich ist die Datei im Arbeitsverzeichnis der laufenden Anwendung.

- Temperaturadresse: zwei hexadezimale Zeichen, Bindestrich, zwölf hexadezimale Zeichen.
- pH/Redox: dezimale I²C-Adresse von 8 bis 119.
- Distanz: `Trigger/Echo`, unterschiedliche BCM-GPIOs von 1 bis 27; im UI keine bereits einem konfigurierten Relais zugeordneten Pins.
- Textfelder: höchstens 64 Zeichen, keine Steuerzeichen; Name darf nicht leer sein. Eine leere Maßeinheit ist erlaubt.
- Formate: .NET-Zahlenformat, z. B. `0.00`, `#0.000` oder `F2`; Vorschau in UI-Sprache bzw. invariant für MQTT. Ungültige Formate und übermäßig große Präzision werden abgewiesen. Leer bedeutet Standardformat.

Die Adress- und Textformatprüfungen beschreiben den modernen Editor, keine vollständige Validierung aller möglichen externen Konfigurationsänderungen. Die Hardwareadresse eines EZO-Sensors wird nicht im Sensor umprogrammiert: geändert wird die Adresse, unter der die Anwendung ihn anspricht.

Prüfung: siehe [UI-Tests](../Tests/Ui/README.md). Die Tests rendern alle sechs Seiten in beiden Sprachen und allen drei Theme-Einstellungen, prüfen den Ansichtswechsel sowie Zahlen-/Konfliktbehandlung. Die Bedienung am echten Raspberry-Pi-Touchdisplay und Hardwarebefehle müssen zusätzlich am Zielgerät geprüft werden.
