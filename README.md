# PoolControl
Ich nehme heute meine neue Poolsteuerung in Betrieb.
Das ganze läfut auf einem Raspberry Pi und ist in .net CORE 6.0 programmiert, es läuft natürlich auch unter Windows, da habe ich entsprechende MOCKs gebaut um die Sensoren zu simulieren. Ich werde das demnächst auf Github unter GPL 3.0 veröffentlichen, Reviewer welcome!
Wen technische Daten interessieren:
Hardware:
- Raspberry Pi 3B+ 1GB	
Waveshare 3,5" TouchDisplay
	
Raspberry Pi GPIO Extension Board der vervierfacht den GPIO Header des Pis (unter dem Display)
	
8-Kanal-Relais 5V
	
Logic Converter TSX0108E, der macht die 3V der GPIOs auf 5V, dann schaltet das Relais zuverlässiger
	
Atlas Ezo Redox Platine (grün, I2C)
	
Atlas Ezo pH Platine (rot, I2C)
	
Gehäuse selbst mit Tinkercad designed und mit Prusa ausgedruckt

Software


	
OS: Raspbian bullseye 5.15.32 mit voller Desktop-Unterstützung
	
.net Core 6.0 Anwendung, in Visual Studio in C# geschrieben
	

		
Avalonia UI Touch-Oberfläche mit XAML und ReactiveUI / Fody
		
Mehrsprachigkeit über Resource-Datei, deutsch und englisch implementiert
		
Serilog als Logger, Console, File, Syslog
		
MQTTnet zur Kommunikation
		
NewtonsoftJSON zur Realisierung der Serialisierung als Json und Konfiguration
		
System.Device.GPIO zur Ansteuerung der GPIO Pins
		
I2C Treiber für die Ezo Ph und Redoy Messung selbst geschrieben (by ESPEasy geklaut, den habe ich aber auch geschrieben )
		
Treiber für GPIO HC-SR04 Ultraschallsensor selbst geschrieben
	
	

Feature:


	
Messung fast beliebig vieler DS18B20 OneWire Temperatursensoren
	
Ansteuerung der 8 Relais unter anderem für Poolfilter, Solarheizung, pH-Einlauf, Salzsteuerung, Poollampe
	
Automatische pH-Dosierung über Messung und Betätigen der pH-Pumpe
	
Automatisches Ein/Ausschalten der Salzanlage in Abhängigkeit der Redox-Messung
	
Automatisches Ein/Ausschalten der Poolfilterpumpe Zeitgesteuert mindestens 2 x 3h / pro bei Temperaturen unter 20°C und bis zu 2 x 4 h am Tag bei Temperaturen > 30 °C (linear gesteuert)
	
Messung des Füllstands meiner Zisterne über HC-SR05 Ultraschallsensor und Umrechnung in Liter
	
Automatische Steuerung der Solarheizung über DS18B20 Temperaturfühler, Ein/Ausschaltung abhängig von der konfigurierbaren Differenztemperatur
	
Spülung der Solarheizung zu einem einstellbaren Zeitpunkt mit einstellbarer Dauer, damit bei bewölkten Tagen das Wasser bewegt wird in der Solarheizung und nicht umkippt
	
sämtliche Messwerte (Temperaturen, pH, Redox, Zisterne) und Zustände der Relais werden über MQTT übermittelt
	
sämtliche Konfigurationsparameter sind über MQTT konfigurierbar
	
alle Relais sind über MQTT steuerbar
	
der pH und Redox-Sensor sind über MQTT komplett steuerbar inklusive Kalibrierung und Ex/Import der Kalibrierung
	
Über MQTT als PoolControl-Thing komplett in mein Openhab integriert
