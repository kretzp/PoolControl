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
using System.Runtime.InteropServices;
using System.Diagnostics;

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

            // RestartReading and Publish
            this.WhenAnyValue(ds => ds.IntervalInSec).Subscribe(value => this.RestartTimerAndPublishNewInterval());

            IntervalInSec = PoolControlConfig.Instance.Settings.PersistenceSaveIntervalInSec;

            _ = PoolMqttClient.Instance;

#if !CREATE
            Data = Persistence.Instance.Load<PoolData>();
#endif
#if CREATE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Data = new PoolData();
                Data.Name = "Winter";
                Data.TemperaturesDict = new Dictionary<string, Temperature>();
                Data.TemperaturesDict.Add("Pool", new Temperature { Address = "28-0114536eebaa", Name = "Pool", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN });
                Data.TemperaturesDict.Add("SolarPreRun", new Temperature { Address = "28-041670557dff", Name = "SolarPreRun", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN });
                Data.TemperaturesDict.Add("SolarHeater", new Temperature { Address = "28-0315a43260ff", Name = "SolarHeater", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN });
                Data.TemperaturesDict.Add("Technikraum", new Temperature { Address = "28-0317609128ff", Name = "TechnicRoom", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 60, Value = double.NaN });
                Data.TemperaturesDict.Add("FrostChecker", new Temperature { Address = "28-0416705896ff", Name = "FrostChecker", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 60, Value = double.NaN });
                Data.SwitchesDict = new Dictionary<string, Switch>();
                Data.SwitchesDict.Add("FilterPump", new Switch { RelayNumber = 8, Name = "FilterPump", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("SolarHeater", new Switch { RelayNumber = 7, Name = "SolarHeater", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("Ph", new Switch { RelayNumber = 6, Name = "PhPump", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("Redox", new Switch { RelayNumber = 5, Name = "RedoxSwitch", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("Poollampe", new Switch { RelayNumber = 4, Name = "PoolLight", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("Three", new Switch { RelayNumber = 3, Name = "Three", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("Two", new Switch { RelayNumber = 2, Name = "Two", HighIsOn = false, On = false });
                Data.SwitchesDict.Add("One", new Switch { RelayNumber = 1, Name = "One", HighIsOn = false, On = false });
                Data.FilterPump = new FilterPump { StandardFilterRunTime = 180, StartMorning = new TimeSpan(8, 0, 0), StartNoon = new TimeSpan(14, 0, 0), FilterOff = new TimeSpan(20, 0, 0) };
                Data.SolarHeater = new SolarHeater { SolarHeaterCleaningTime = new TimeSpan(21, 30, 0), SolarHeaterCleaningDuration = 180, TurnOnDiff = 6.0, TurnOffDiff = 3.0, MaxPoolTemp = 29.5 };
                Data.Ph = new Ph { Name = "pHValue", MaxValue = 7.3, AcidInjectionDuration = 20, AcidInjectionRecurringPeriod = 10, IntervalInSec = 60, Address = "99", LedOn = true, ViewFormat = "#0.00", InterfaceFormat = "#0.000", UnitSign = "pH", Value = double.NaN };
                Data.Redox = new Redox { Name = "RedoxValue", On = 750, Off = 840, IntervalInSec = 60, Address = "98", LedOn = true, ViewFormat = "#0", InterfaceFormat = "#0.0", UnitSign = "mV", Value = double.NaN };
                Data.Distance = new Distance { Address = "16/26", Name = "Distance", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "cm", NameL = "Volume", ViewFormatL = "#0", InterfaceFormatL = "#0", UnitSignL = "L", IntervalInSec = 60, Value = double.NaN, NumberOfMeasurements = 5 };
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
                Data.WinterMode = false;
            }
            else
            {
                Data = Persistence.Instance.Load<PoolData>();
            }
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

            Data.FilterPump.Switch = Data.SwitchesDict[Data.FilterPump.GetType().Name];
            Data.SolarHeater.Switch = Data.SwitchesDict[Data.SolarHeater.GetType().Name];
            Data.Redox.Switch = Data.SwitchesDict[Data.Redox.GetType().Name];
            Data.Redox.FilterPumpSwitch = Data.SwitchesDict[Data.FilterPump.GetType().Name];
            Data.Ph.Switch = Data.SwitchesDict[Data.Ph.GetType().Name];
            Data.Ph.FilterPumpSwitch = Data.SwitchesDict[Data.FilterPump.GetType().Name];

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
            Data.TemperaturesDict["Pool"].MeasurmentTaken += Data.FilterPump.OnTemperatureChange;
            
            // Fire Event for SolarHeater Temperature change für calculation SolarHeater Switch On/Off
            Data.TemperaturesDict["SolarPreRun"].MeasurmentTaken += Data.SolarHeater.OnTemperatureChange;
            Data.TemperaturesDict[Data.SolarHeater.GetType().Name].MeasurmentTaken += Data.SolarHeater.OnTemperatureChange;

#if CREATE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Persistence.Instance.Save(Data);
            }
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

        protected override void OnTimerTicked(object? state)
        {
            Persistence.Instance.Save(Data);

            Process currentProc = Process.GetCurrentProcess();
            double bytesInUse = currentProc.PrivateMemorySize64 / 1024 / 1024;

            Logger.Debug("MBytes in use {bytes} in {proc}", bytesInUse, currentProc.ProcessName);
        }

        [Reactive][JsonProperty]
        public PoolData Data { get; set; }
    }
}
