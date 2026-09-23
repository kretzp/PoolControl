# PoolControl Touch — Oberflächendesignvorschlag

> Historischer Entwurf vor der Umsetzung. Die tatsächlich implementierten Funktionen,
> Abweichungen und Grenzen stehen in [Modern-UI.md](Modern-UI.md) und im
> [Checkpoint](Checkpoint-Modern-UI.md). Aussagen über noch ausstehende Umsetzung
> im folgenden Entwurf beziehen sich auf dessen ursprünglichen Erstellungszeitpunkt.

Stand: 23. September 2026. Ziel: native Avalonia-Oberfläche, 1024 × 600, Querformat und Touch. Dieses Dokument beschreibt den vollständigen Entwurf; die produktive Oberfläche wurde noch nicht umgebaut. Der interaktive Entwurf verwendet Beispieldaten. Detaildialoge für Wartung und Kalibrierung sind konzeptionelle Vorschauen.

## 1. Gestaltung

Ruhige, helle Flächen oder tiefes Blau im Dunkelmodus, Türkis als durchgehender Akzent, große Zahlen, klare Typografie, dezente Konturen. Keine Regenbogenfarben pro Sensor. Status immer mit Text und Symbol zusätzlich zur Farbe. Opaque Flächen statt aufwendigem Blur; kurze Zustandswechsel statt dauernder Animationen.

| Merkmal | Festlegung |
|---|---|
| Vollbild | 1024 × 600 logische Pixel bei 100 % Skalierung, ohne Fensterrahmen |
| Aufteilung | Kopf 64 px, Inhalt 456 px, untere Navigation 80 px |
| Inhaltsfläche | 24 px Seitenabstand, 20 px oben/unten; 16 px Abstand zwischen Panels |
| Raster | 8 px; zwei Spalten für Status und Bedienung |
| Touch | mindestens 52 × 52 px; primäre Aktionen 56–64 px; mindestens 8 px Abstand |
| Schrift | Segoe UI unter Windows, mitgelieferte Noto Sans für Linux; 16–18 px Standard, 14 px sekundär, 26 px Titel, 48–64 px Hauptwert |
| Rundung | 18 px Panels, 12 px Controls; keine Schatten auf jedem Control |
| Messwerte | tabellarische Ziffern, Einheit getrennt, gleiche Dezimalstellen pro Sensor |
| Zustandswechsel | 120–180 ms, reduzierte Bewegung berücksichtigen |

Der aktuelle MainView ist 1024 × 568. Für ein echtes 1024 × 600 Touchgerät wird ein rahmenloses Vollbildfenster vorgesehen; verfügbare Clientfläche und Betriebssystemskalierung werden am Zielgerät geprüft. Keine globale Viewbox-Skalierung, die Touchziele verkleinert. Bei 568 px Clienthöhe muss der Inhaltsbereich entsprechend auf 424 px reagieren; seltene Einstellungen dürfen dann gezielt scrollen.

## 2. Menüführung

Sechs dauerhaft sichtbare Tabs unten, jeweils Symbol plus Text, etwa 159 px breit. Aktiver Tab: Akzentfläche und deutlich markierter Text. Kein Hamburger-Menü für Hauptfunktionen, kein horizontal scrollendes Tabband.

| Tab | Erste Ansicht | Zweite Ebene |
|---|---|---|
| Übersicht | Pooltemperatur, pH, Redox, Filterstatus, Zisternenvolumen, Poollicht | Messwert antippen öffnet den zuständigen Tab; Temperaturdetails führen zu Sensoren |
| Filter | Ist-Zustand, Auto/Ein/Aus, temperaturabhängige Laufzeit | Tagesplan mit Morgenstart, Nachmittagsstart, spätestem Ende, Basislaufzeit; nächste Start-/Endzeit |
| Solar | Pool/Kollektor, Temperaturdifferenz, Heizstatus | Regelung: Maximum, Ein-/Ausschaltdifferenz; Spülung: Uhrzeit und Dauer; manuelle Bedienung über Betriebsartdialog |
| Wasser | Umschaltung pH / Redox mit dauerhaft sichtbarem aktuellen Wert | Regelparameter, Sensorinformationen, geführte Kalibrierung, manuelle Funktion |
| Zisterne | Liter und Prozent, gut lesbarer Füllbalken | Sensorabstand, Aktualität, Tankkonfiguration und technische Details |
| System | Anzeige/Sprache und Betrieb/Wartung | Alle Sensoren, Relais, Winterbetrieb, MQTT/Diagnose, Meldungen, Info, Beenden |

Kopfleiste: Produktname, Verbindungsstatus, Sprache und Theme. Messwertalter gehört zum Messwert; ein MQTT-Verbindungsstatus allein beweist keine aktuellen Sensorwerte. Alle fünf vorhandenen Temperatursensoren bleiben unter System → Sensoren erreichbar. Bezeichnungen stammen aus der Konfiguration; die Namen im Entwurf sind Beispiele.

Maximal zwei Navigationsebenen. Unterseiten erhalten eine große Zurück-Schaltfläche und behalten den Haupttab. Rückkehr bewahrt die zuletzt gewählte Unterseite. Häufige Seiten passen ohne vertikales Scrollen in 600 px; umfangreiche Diagnose erhält eigene Unterseiten.

## 3. Controls und Bedienung

**Messwertkachel:** Name, großer Wert, Einheit, Aktualität und bei vorhandener Regeldefinition ein Status. Tappbare Kacheln haben einen sichtbaren Detailhinweis. Redox wird als Redoxpotential angezeigt; daraus wird keine unbelegte Aussage zur Wasserqualität abgeleitet.

**Betriebsart:** Segmentauswahl Auto / Ein / Aus. Der Ist-Zustand ist davon getrennt: eine angeforderte Aktion darf erst nach Rückmeldung als wirksam erscheinen. Zeitlich begrenzte manuelle Übersteuerung mit Rückkehr zur Automatik ist eine vorgeschlagene neue Funktion und muss vor Umsetzung gegen die Steuerungslogik geprüft werden. Der Entwurf simuliert lediglich die Auswahl.

**Schalter:** ToggleSwitch für eindeutige binäre Funktionen wie Poollicht oder Sensor-LED; Text Ein/Aus bleibt sichtbar. Anforderung ausstehend: „Wird geschaltet …“, Rückmeldung fehlend: „Nicht bestätigt“. Hardwarebedingte Sperren werden mit konkretem Grund dargestellt.

**Zahlen:** Antippen öffnet einen fokussierten Eingabedialog mit Bezeichnung, aktuellem Wert, Einheit, Bereich, großen Minus-/Plus-Flächen und Zifferneingabe. Abbrechen und Übernehmen immer sichtbar. Eine eigene Touch-Zifferntastatur ist für Linux/Kiosk einzuplanen. Änderungen bleiben bis Übernehmen lokal. Keine Slider für pH, Redox oder Dosierdauer. Grenzen und Schrittweiten aus den vorhandenen Controls übernehmen und durch die Fachlogik validieren; keine neuen Standardwerte aus dem Design ableiten.

**Zeit:** Großer Zeit-Button, im nativen UI Touchdialog mit Stunde/Minute und separaten Plus-/Minus-Tasten. Optional beschriftete Tageszeitleiste als Ergänzung, niemals als einziger Editor. 24-Stunden-Format als Standard. Der Webentwurf verwendet nur zur Demonstration das Zeitfeld des Browsers.

**Validierung:** Fehler direkt am Feld; Wert bleibt zur Korrektur erhalten. Abhängige Grenzen ebenfalls prüfen, etwa Redox Ein < Aus und Solar Aus-Differenz < Ein-Differenz, soweit dies der vorhandenen Regelungssemantik entspricht. Externe MQTT-Änderung während einer Bearbeitung erzeugt einen sichtbaren Konflikt mit Auswahl „Neu laden“ oder „Eigene Änderung übernehmen“.

**Bestätigung:** Normale Anzeigepräferenzen direkt übernehmen. Manuelle Dosierung, Kalibrierung löschen, Winterbetrieb und Beenden erhalten eine konkrete Bestätigung mit Wirkung und den betroffenen Funktionen. Keine Bestätigungsflut bei harmloser Navigation.

## 4. Wasserpflege und Wartung vollständig abbilden

### pH
Regelseite: Obergrenze, Messintervall, Wiederholintervall, Dosierdauer, Ist-Zustand der Dosierung. Sensorseite: Name, Adresse, Einheit, Zeitstempel, Versorgung, Formatparameter, LED und Identifizieren. Messwert und Zeitstempel sind lesbar, nicht versehentlich editierbar.

Kalibrierung als eigene Unterseite mit Schrittanzeige: Vorbereitung → mittlerer Referenzpunkt → niedriger Referenzpunkt → hoher Referenzpunkt → Ergebnis. Verwendbare Punkte gemäß tatsächlichem Sensorablauf anbieten. Pro Schritt: Referenzwert, aktueller Sensorwert, kurze Anleitung, explizite Aktion, Warte-/Ergebniszustand. Zurück, Abbrechen und Wiederholen berücksichtigen. Vorhandene Funktionen für Steigung, Kalibrierstatus und Löschen bleiben verfügbar; Löschen liegt in einem separaten Wartungsdialog.

### Redox
Regelseite: Ein-/Ausschaltschwelle, Messintervall, Ist-Zustand der Elektrolyse. Sensorseite analog pH. Kalibrierung mit Referenzwert, Fortschritt/Rückmeldung, Ergebnis, Wiederholen und Abbrechen; Status, Identifizieren und Löschen separat erreichbar.

### Temperatur und Zisterne
Temperatursensoren: alle vorhandenen Messstellen, Anzeigenamen, Adresse, Messintervall, Zeitstempel, Format-/Einheitenparameter. Zisterne: Abstand und Liter bleiben erhalten. Prozentwert nur anzeigen, wenn eine belastbare Kapazität vorliegt; sonst Liter und Abstand ohne erfundene Prozentanzeige. Tankkonfiguration nur anbieten, soweit sie durch das Modell unterstützt oder gesondert ergänzt wird.

### System
Relaisliste bildet alle konfigurierten Ausgänge mit sprechendem Namen und tatsächlichem Zustand ab. Winterbetrieb erklärt seine konkrete Wirkung anhand der vorhandenen Steuerung. Diagnose bündelt MQTT, Sensorfehler, Zeitstempel, Logs und Version. Beenden ist hier sichtbar auffindbar und ersetzt die unauffällige Exit-Fläche der alten Übersicht.

## 5. Themes

| Token | Hell | Dunkel |
|---|---|---|
| Hintergrund | #EDF3F5 | #101D26 |
| Panel | #FFFFFF | #1B2B36 |
| Haupttext | #163441 | #ECF4F7 |
| Sekundärtext | #506976 | #A9BDC8 |
| Akzent | #08786F | #6EDACF |
| Akzentfläche | #E6F4F3 | #203F43 |
| Kontur | #DBE5E9 | #344853 |

Auswahl Hell / Dunkel / System dauerhaft speichern, ohne Neustart anwenden. Hell ist für helle Umgebung vorgesehen, Dunkel für Abendbetrieb. Blau ist eine optionale Designalternative im Entwurf. Ein zusätzlicher Kontrastmodus wird bei Bedarf als eigene Variante mit kräftigeren Konturen umgesetzt; er ist im Prototyp noch nicht enthalten. Alle Zustände einschließlich Dialoge, disabled, focus und Fehlermeldungen in beiden Themes prüfen.

## 6. Sprache

Deutsch und Englisch zum Start, Sprachnamen statt Flaggen. Wechsel sofort über die Kopfzeile oder System → Anzeige. Aktiver Tab, Eingaben und Anlagenzustand bleiben erhalten. Auch Dialoge, Validierungsfehler, Statusbeschreibungen und barrierefreie Namen werden übersetzt. Benutzerdefinierte Sensornamen werden nicht automatisch übersetzt.

Die vorhandenen RESX-Dateien weiterverwenden. Statische x:Static-Verweise für laufzeitveränderliche Texte durch Bindings an einen zentralen LocalizationService mit Änderungsbenachrichtigung ersetzen. UI-Kultur und Formatkultur setzen; Dezimalzeichen und Datumsformat passend darstellen. MQTT-Topics, Persistenzschlüssel und Protokollwerte bleiben kulturunabhängig. Längere englische Bezeichnungen bei 1024 × 600 prüfen; keine abgeschnittenen Tabtitel.

## 7. Zustände

| Zustand | Darstellung / Verhalten |
|---|---|
| Normal | Wert, Einheit und Aktualität sichtbar; Status in Text |
| Wert veraltet | Letzten Wert mit „Veraltet · …“ markieren; keine normale grüne Statusanzeige |
| Sensorfehler | „—“ oder ausdrücklich letzter bekannter Wert, Fehlerhinweis und Details |
| MQTT getrennt | Verbindungsbanner; lokale Steuerung nur entsprechend tatsächlicher Architektur verfügbar |
| Schaltanforderung | Ausstehend kennzeichnen, widersprüchliche Mehrfachbefehle verhindern |
| Fehler beim Speichern | Entwurf behalten, erneutes Übernehmen anbieten |
| Kritische Meldung | Priorisierte, nicht automatisch verschwindende Meldung; Quittieren beseitigt nicht die Ursache |
| Laden | Beschriftete Platzhalter, keine erfundenen Nullwerte |

Die Grenze für „veraltet“ richtet sich nach dem konfigurierten Messintervall und der fachlich festgelegten Toleranz. Keine festen Zeitgrenzen allein aus dem Design ableiten.

## 8. Umsetzung mit Avalonia

Empfehlung: vorhandenes FluentTheme als Basis und ein durchgängiges PoolControl-Designsystem. Die gewünschte Optik braucht keine zusätzliche Komplettbibliothek. SukiUI ist im Projekt referenziert, wird aber nicht als Voraussetzung des Entwurfs verwendet. Vor einer Implementierung die aktuell gemischten Avalonia-Paketstände separat auf Kompatibilität prüfen.

| Entwurf | Native Umsetzung |
|---|---|
| Hauptnavigation | TabControl mit unterer TabStrip-Anordnung und eigenem ControlTheme |
| Messwert / Status | wiederverwendbare MeasurementTile und StatusBadge als UserControls |
| Auto / Ein / Aus | gestylte RadioButton-Gruppe mit exklusiver Auswahl |
| Binärfunktion | ToggleSwitch mit beschriftetem Zustand |
| Zahleneditor | NumericUpDown mit großem Template plus Touch-Ziffernfeld |
| Zeitdialog | TimePicker beziehungsweise Touch-Template mit expliziter Bestätigung |
| Füllstand | ProgressBar mit Liter-/Prozentbeschriftung |
| Dialoge | zentrales Overlay im selben Fenster, Fokusbindung und Rückgabe an Auslöser |
| Themes | RequestedThemeVariant, ThemeDictionaries und DynamicResource für Farb-/Formtokens |
| Sprache | RESX + LocalizationService + beobachtbare Bindings |

Avalonia unterstützt helle und dunkle Varianten im FluentTheme sowie dynamische Ressourcen für den Laufzeitwechsel: [Theme variants](https://docs.avaloniaui.net/docs/styling/theme-variants), [Theme switching](https://docs.avaloniaui.net/docs/how-to/theme-switching-how-to).

Geplante Struktur: ShellView/ShellViewModel, je ein ViewModel pro Hauptseite, ThemeService, LocalizationService, DialogService und wiederverwendbare Controls. Steuerungslogik bleibt die fachliche Quelle; editierbare Formulare arbeiten mit einem Entwurf und übergeben Änderungen erst bei Übernehmen. Keine GPIO-/MQTT-Logik in Styles oder Control-Templates.

## 9. Abnahme der späteren Implementierung

- Alle sechs Tabs, beide Sprachen und alle Themevarianten bei 1024 × 600 ohne Überlagerung prüfen; Touchziele am echten Display testen.
- Übersicht und primäre Regelungsseiten ohne Scrollzwang; Dialogaktionen auch bei Bildschirmtastatur erreichbar.
- Mindestens 4,5:1 Kontrast für normalen Text und 3:1 für große Schrift/entscheidende UI-Markierungen messen.
- Bedienbarkeit ohne Hover, sichtbarer Tastaturfokus, sinnvolle AutomationProperties.Name, Fokusführung und Rückkehr bei Dialogen.
- Alle vorhandenen Sensoren, Schalter, Regelparameter und Kalibrierbefehle über die neue Navigation erreichbar.
- Sensorfehler, veraltete Werte, MQTT-Ausfall, abgewiesene Befehle und Persistenzfehler darstellen und prüfen.
- Theme/Sprache nach Neustart wiederherstellen; keine verloren gegangenen Entwürfe beim Wechsel.
- Automatische Regelung, Hardwarezugriff und bestehende Prüfungen unverändert funktionsfähig halten.

Umsetzungsfolge: Shell + Theme/Sprachdienste → Übersicht → Filter/Solar → Wasser/Kalibrierung → Zisterne/System → Zustandsprüfung und Touchabnahme auf dem Raspberry Pi.
