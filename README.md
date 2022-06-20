# PoolControl

Update: This software ist production now in my pool environment for about 1 month without any issues ;-)

This is a brandnew Pool Control Software based on an Raspberry Pi, coded in .net CORE 6.0. It runs with mocks in an Windows Environment.
Hardware:
- Raspberry Pi 3B+ 1GB
- Waveshare 3,5" TouchDisplay
- Raspberry Pi GPIO Extension Board 4x GPIO Header of Pi (beyond the Display)
- 8-Kanal-Relais 5V
- Logic Converter TSX0108E, to convert 3V of GPIOs to 5V for relay switching
- Atlas Ezo Redox Platine (blue, I2C)
- Atlas Ezo pH Platine (red, I2C)
- Housing designed with Tinkercad and printed with Prusa

Software	
- OS: Raspbian bullseye 5.15.32 mit voller Desktop-Unterstützung
- .net Core 6.0, in Visual Studio C#
- Avalonia UI Touch-UI with XAML and ReactiveUI / Fody
- Mulit Language with Resource-File, german und englisch implemented
- Serilog als Logger, Console, File, Syslog
- MQTTnet for Communication
- NewtonsoftJSON for serializing Json and Configuration
- System.Device.GPIO for GPIO handling
- I2C Driver for Ezo Ph and Redox Measurement
- Driver für GPIO HC-SR04 Sensor

Features:
- Measurement oft DS18B20 OneWire temperature sensors
- 8-channel relay hanbdlong for pool filter, solar heater, redox, pH and pool lamp
- automatic pH acid dosing
- automatic redox handling (salt electrolyse)
- turn pool filtre on off depending on temperature and cinfigured times 2 x 3h / day at temperatures below 20°C and up to 2 x 4 h a day at temperatures > 30 °C (linear)
- Measurment of filling of my rain water cistern
- automaitc handling ofsolar heater over DS18B20 sensor
- cleaning of solar heater at s specified time per day
- All measurments will be transfered over MQTT (temperatures, pH, redox, cistern)
- all configuration parameters are abel to be configured over MQTT
- all relays could be switched over MQTT
- pH and redox sSensor could be handled over MQTT, including calibration
- I integrated this in Openhab as generic MQTT Thing

The printable .stl files could be found here:
https://www.thingiverse.com/thing:5383565

Impressions:

![image](https://user-images.githubusercontent.com/15065072/168045938-3de59ce1-81f1-497f-ac0f-714105303de1.png)

![image](https://user-images.githubusercontent.com/15065072/168045991-5ee91ce0-a7f1-400e-a9b7-eb51fb8ae487.png)

![image](https://user-images.githubusercontent.com/15065072/168046041-2a5c2fb3-a5a2-4c48-a082-4280c84ddcfd.png)

![image](https://user-images.githubusercontent.com/15065072/168046065-fad64f4a-37bb-413a-8254-2bc93106f913.png)

![image](https://user-images.githubusercontent.com/15065072/168046088-419ad53e-7598-41af-8cf7-e962308e4593.png)

![image](https://user-images.githubusercontent.com/15065072/168046108-d9cad216-3cf1-4f39-8448-e658798d0bcf.png)

![image](https://user-images.githubusercontent.com/15065072/168046364-94caa5de-7882-41be-a0a1-73f5d06769ac.png)

![image](https://user-images.githubusercontent.com/15065072/168046420-1908465c-4d24-4caf-b76f-63fbd81dd368.png)

