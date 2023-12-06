# PoolControl

Update: This software is in production in my pool environment for about 1 year without any issues ;-)

This is a brandnew Pool Control Software based on an Raspberry Pi, coded in .net CORE 8.0. It runs with mocks in an Windows Environment.
Hardware:
- Raspberry Pi 4 4GB (Raspberry Pi 3B+ 1GB is sufficient)
- Waveshare 3,5" TouchDisplay
- Raspberry Pi GPIO Extension Board 4x GPIO Header of Pi (beyond the Display)
- 8-Kanal-Relais 5V
- Logic Converter TSX0108E, to convert 3V of GPIOs to 5V for relay switching
- Atlas Ezo Redox Platine (blue, I2C)
- Atlas Ezo pH Platine (red, I2C)
- Housing designed with Tinkercad and printed with Prusa

Software	
- OS: Raspbian bullseye 5.15.32 mit voller Desktop-Unterstützung
- .net Core 8.0, in Visual Studio C#
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
- Notification of manual inputs with related MQTT topigs

The printable .stl files could be found here (Housing for Raspberry Pi 3B+ an 4):
https://www.thingiverse.com/thing:5383565

To install, I assume, that you have a raspberry pi running with a Desktop environment. If not, checkout https://www.raspberrypi.com/software/ an download the imager and install latest raspberry pi os (32bit) with Desktop environment (bookworm is not working with waveshare at the moment but bullseye does!).
To install the Waveshare 3,5" TouchDisplay follow the instructions on the waveshare page. I use 3.5 inch RPi LCD (V) Rev2.0:
https://www.waveshare.com/wiki/3.5inch_RPi_LCD_(B)



Impressions:

![image](https://github.com/kretzp/PoolControl/assets/15065072/2dc5f03b-1807-45f2-bb3f-ea02dfd2967f)

![image](https://github.com/kretzp/PoolControl/assets/15065072/63a1f573-4b01-465d-8af1-2496c12a3844)

![image](https://github.com/kretzp/PoolControl/assets/15065072/c9fff691-70e9-4426-a3c4-d86c1c20634d)

![image](https://github.com/kretzp/PoolControl/assets/15065072/6d1050f5-38b2-412e-ab47-f6d4be4b2417)

![image](https://github.com/kretzp/PoolControl/assets/15065072/6438ee57-2da2-4406-a2d2-8eba6c2a4df2)

![image](https://github.com/kretzp/PoolControl/assets/15065072/a1b13269-6270-4a0a-9552-8a6d2491c3bf)

![image](https://github.com/kretzp/PoolControl/assets/15065072/307455a6-1e81-41f9-8ff8-8a3c08fdd153)

![image](https://github.com/kretzp/PoolControl/assets/15065072/b96233f3-0341-431d-82db-6f873daf9049)

![image](https://user-images.githubusercontent.com/15065072/168046420-1908465c-4d24-4caf-b76f-63fbd81dd368.png)

