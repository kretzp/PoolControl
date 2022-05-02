#define CREATEN
using MQTTnet.Client;
using ReactiveUI.Fody.Helpers;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using PoolControl.Helper;
using PoolControl.Communication;
using Avalonia.Interactivity;
using ReactiveUI;
using System.Reactive;
using PoolControl.Hardware;

namespace PoolControl.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private static readonly char PROPERTY_SPLITTER = '/';
        private bool disposedValue;

        public ReactiveCommand<Unit, Unit> CloseWindow { get; }


        public MainWindowViewModel()
        {
            Logger = Log.Logger?.ForContext<MainWindowViewModel>() ?? throw new ArgumentNullException(nameof(Logger));

            Logger.Information("#################################################");
            Logger.Information("########### Program started #####################");
            Logger.Information("#################################################");

            CloseWindow = ReactiveCommand.Create(Close_Button_Clicked);

            _ = PoolMqttClient.Instance;

#if !CREATE
            Data = Persistence.Instance.Load<PoolData>();
#endif
#if CREATE
            Data = new PoolData();
            Data.TemperaturesDict = new Dictionary<string, Temperature>();
            Data.TemperaturesDict.Add("Pool", new Temperature { Key = "Pool", Address = "28-0317600537ff", Name = "Pool", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN});
            Data.TemperaturesDict.Add("Solarzulauf", new Temperature { Address = "28-ff7d557016458", Name = "Solarzulauf", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN });
            Data.TemperaturesDict.Add("Solarheizung", new Temperature { Address = "28-0315a43260ff", Name = "Solarheizung", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN });
            Data.TemperaturesDict.Add("Technikraum", new Temperature { Address = "28-ff28916017325", Name = "Technikraum", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 60, Value = double.NaN });
            Data.TemperaturesDict.Add("Frostwaechter", new Temperature { Key = "Frostwaechter", Address = "28-ff9658701646f", Name = "Frostwächter", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 60, Value = double.NaN });
            Data.SwitchesDict = new Dictionary<string, Switch>();
            Data.SwitchesDict.Add("Filterpumpe", new Switch{ RelayNumber = 8, Name = "Filterpumpe", HighIsOn = false, On = false });
            Data.SwitchesDict.Add("Solarheizung", new Switch { RelayNumber = 7, Name = "Solarheizung", HighIsOn = false, On = false });
            Data.SwitchesDict.Add("Ph", new Switch { RelayNumber = 6, Name = "pH-Pumpe", HighIsOn = false, On = false });
            Data.SwitchesDict.Add("Redox", new Switch { RelayNumber = 5, Name = "Salzanlage", HighIsOn = false, On = false });
            Data.SwitchesDict.Add("Poollampe", new Switch { RelayNumber = 4, Name = "Poollampe", HighIsOn = false, On = false });
            Data.Filterpumpe = new Filterpumpe { StandardFilterlaufzeit = 180, StartVormittags = new TimeSpan(8, 0, 0), StartNachmittags = new TimeSpan(14, 0, 0) };
            Data.Solarheizung = new Solarheizung { Spuelzeitpunkt = new TimeSpan(21, 30, 0), Spueldauer = 180, EinschaltDiff = 6.0, AusschaltDiff = 3.0, MaxPoolTemp = 29.5 };
            Data.Ph = new Ph { Name="pH-Wert", MaxValue = 7.4, Einlaufdauer = 20, EinlaufdauerInterval = 10, IntervalInSec = 60, Address = "99", LedOn = true, ViewFormat = "#0.0", InterfaceFormat = "#0.000", UnitSign = "pH", Value = double.NaN };
            Data.Redox = new Redox { Name="Redox-Wert", Ein = 750, Aus = 840, IntervalInSec = 60, Address = "98", LedOn = true, ViewFormat = "#0", InterfaceFormat = "#0.0", UnitSign = "mV", Value = double.NaN };
            Data.Distance = new Distance { Address = "16/26", Name = "Abstand", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "cm", NameL = "Volumen", ViewFormatL = "#0", InterfaceFormatL = "#0", UnitSignL = "L", IntervalInSec = 10, Value = double.NaN, NumberOfMeasurements = 5 };
            Data.RelayConfig = RelayConfig.Instance;
            Data.RelayConfig.RelayToLogicLevelConverterDict = new Dictionary<int, int>();
            for (int i = 1; i < 9; i++)
            {
                Data.RelayConfig.RelayToLogicLevelConverterDict[i] = i;
            }
            Data.RelayConfig.LogicLevelConverterToGpioDict = new Dictionary<int, int>();
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(1, 19);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(2, 13);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(3, 5);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(4, 22);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(5, 27);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(6, 23);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(7, 20);
            Data.RelayConfig.LogicLevelConverterToGpioDict.Add(8, 21);

#endif            
            // Set RelayConfig as Singleton
            RelayConfig.Instance = Data.RelayConfig;

            // Populate Switches
            Data.Switches = new List<Switch>();
            Data.SwitchesObj = new Dictionary<string, object>();
            foreach(var sw in Data.SwitchesDict)
            {
                Switch swi = (Switch)sw.Value;
                swi.Key = sw.Key;
                Data.Switches.Add(swi);
                Data.SwitchesObj.Add(swi.Key, swi);
            }

            Data.Filterpumpe.Switch = Data.SwitchesDict[Data.Filterpumpe.GetType().Name];
            Data.Solarheizung.Switch = Data.SwitchesDict[Data.Solarheizung.GetType().Name];
            Data.Redox.Switch = Data.SwitchesDict[Data.Redox.GetType().Name];
            Data.Ph.Switch = Data.SwitchesDict[Data.Ph.GetType().Name];

            // Populate Temperatures
            Data.Temperatures = new List<Temperature>();
            Data.TemperaturesObj = new Dictionary<string, object>();
            foreach (var t in Data.TemperaturesDict)
            {
                Temperature te = (Temperature)t.Value;
                te.Key = t.Key;
                Data.Temperatures.Add(te);
                Data.TemperaturesObj.Add(te.Key, te);
            }

            // Set PoolTemperature for pH correction and Filter Time
            Data.Ph.PoolTemperature = Data.TemperaturesDict["Pool"];

            // Fire Event for Pool Temperature change for calculationg Filterlaufzeit
            Data.TemperaturesDict["Pool"].MeasurmentTaken += Data.Filterpumpe.OnTemperatureChange;
            
            // Fire Event for Solarheizung Temperature change für calculation Solarheizung Switch On/Off
            Data.TemperaturesDict["Solarzulauf"].MeasurmentTaken += Data.Solarheizung.OnTemperatureChange;
            Data.TemperaturesDict[Data.Solarheizung.GetType().Name].MeasurmentTaken += Data.Solarheizung.OnTemperatureChange;

#if CREATE
            Persistence.Instance.Save(Data);
#endif
            // Open GPIO and handle Switches
            Data.OpenGpioSwitches();
            foreach (var sw in Data.Switches)
            {
                sw.switchRelay();
            }

            // Open Trigger and Echo Gpio
            Data.OpenGpioEchoAndTrigger();

            // Subscribe events after loading Data, so that loading does not fire events!
            PoolMqttClient.Instance.register(MqttClient_ApplicationMessageReceivedAsync);

            Logger.Information("MainWindowViewModel Initialized");
        }

        // Close Gpio in Destruction
        ~MainWindowViewModel()
        {
            Data.CloseGpioSwitches();
            Data.CloseGpioEchoAndTrigger();
        }

        private void Close_Button_Clicked()
        {
            Persistence.Instance.Save(Data);

            Data.GpioSwitchesOff();
            Data.CloseGpioSwitches();
            Data.CloseGpioEchoAndTrigger();
            Gpio.Instance.dispose();

            PoolMqttClient.Instance.Disconnect();

            Thread.Sleep(3000);

            Logger.Information("-----------------------------------------------------");
            Logger.Information("---------------------- Closing Application ----------");
            Logger.Information("-----------------------------------------------------");

            ((Serilog.Core.Logger)Logger).Dispose();

            //Dispose();

            App.MainWindow.Close();
        }

        private Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            Result result;
            object baseObject = Data;
            string propertyName = "";
            string propertyValue = "";
            string objectNameToSet = "";
            string key = "";

            propertyValue = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
            string topic = arg.ApplicationMessage.Topic.Replace(PoolControlConfig.Instance.Settings.BaseTopic.Command, "");

            if (topic.ToLower().Equals("sendstate"))
            {
                publishMessage("json", Persistence.Instance.Serialize(Data));
            }
            else if (topic.ToLower().StartsWith("i2c/"))
            {
                string deviceName = topic.Split("/")[1];
                BaseMeasurement i2cObject = ((MeasurementModelBase)Data.GetType().GetProperty(deviceName).GetValue(Data)).BaseMeasurement;
                MeasurementResult mr;
                try
                {
                    mr  = (MeasurementResult)i2cObject.GetType().GetMethod("send_i2c_command").Invoke(i2cObject, new object[] { propertyValue });
                }
                catch (Exception ex)
                {
                    mr = new MeasurementResult { Result = 99, ReturnCode = 99, StatusInfo = ex.Message, TimeStamp = DateTime.Now };
                }

                publishMessage(topic, Persistence.Instance.Serialize(mr), (int)arg.ApplicationMessage.QualityOfServiceLevel, arg.ApplicationMessage.Retain);
            }
            else
            {

                try
                {
                    if (topic.Contains(PROPERTY_SPLITTER))
                    {
                        string[] properties = topic.Split(PROPERTY_SPLITTER);

                        objectNameToSet = properties[0];

                        if (properties.Length == 2)
                        {
                            propertyName = properties[1];
                        }
                        else
                        {
                            key = properties[1];
                            propertyName = properties[2];
                        }
                    }
                    else
                    {
                        propertyName = topic;
                    }

                    result = PropertySetter.setProperty(baseObject, propertyName, propertyValue, objectNameToSet, key);

                    if (result.Success)
                    {
                        Logger.Debug(result.Message);
                    }
                    else
                    {
                        Logger.Error(result.Message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error: topic: {arg.ApplicationMessage.Topic} payload: {arg.ApplicationMessage.Payload}");
                }
            }

            return arg.AcknowledgeAsync(new CancellationToken());
        }

        [Reactive][JsonProperty]
        public PoolData Data { get; set; }
    }
}
