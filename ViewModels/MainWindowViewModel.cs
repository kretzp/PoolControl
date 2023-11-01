#define CREATEN
using MQTTnet.Client;
using ReactiveUI.Fody.Helpers;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using PoolControl.Communication;
using ReactiveUI;
using System.Reactive;
using PoolControl.Hardware;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PoolControl.Helper;
using Avalonia.Controls.Notifications;
using PoolControl.Views;

namespace PoolControl.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private static readonly char PropertySplitter = '/';

    public ReactiveCommand<Unit, Unit> CloseWindow { get; }


    public MainWindowViewModel()
    {
        Logger = Log.Logger?.ForContext<MainWindowViewModel>() ?? throw new ArgumentNullException(nameof(Logger));

        Logger.Information("#################################################");
        Logger.Information("########### Program started #####################");
        Logger.Information("#################################################");

        CloseWindow = ReactiveCommand.Create(Close_Button_Clicked);

        // RestartReading and Publish
        this.WhenAnyValue(ds => ds.IntervalInSec).Subscribe(_ => this.RestartTimerAndPublishNewInterval());

        IntervalInSec = PoolControlConfig.Instance.Settings!.PersistenceSaveIntervalInSec;

        _ = PoolMqttClient.Instance;

#if !CREATE
        Data = Persistence.Instance.Load<PoolData>();
#endif
#if CREATE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
            Data = new PoolData
            {
                Name = "Winter",
                TemperaturesDict = new Dictionary<string, Temperature>
                {
                    { "Pool", new Temperature { Address = "28-0114536eebaa", Name = "Pool", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN } },
                    { "SolarPreRun", new Temperature { Address = "28-041670557dff", Name = "SolarPreRun", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN } },
                    { "SolarHeater", new Temperature { Address = "28-0315a43260ff", Name = "SolarHeater", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 10, Value = double.NaN } },
                    { "Technikraum", new Temperature { Address = "28-0317609128ff", Name = "TechnicRoom", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 60, Value = double.NaN } },
                    { "FrostChecker", new Temperature { Address = "28-0416705896ff", Name = "FrostChecker", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "°C", IntervalInSec = 60, Value = double.NaN } }
                },
                SwitchesDict = new Dictionary<string, Switch>
                {
                    { "FilterPump", new Switch { RelayNumber = 8, Name = "FilterPump", HighIsOn = false, On = false } },
                    { "SolarHeater", new Switch { RelayNumber = 7, Name = "SolarHeater", HighIsOn = false, On = false } },
                    { "Ph", new Switch { RelayNumber = 6, Name = "PhPump", HighIsOn = false, On = false } },
                    { "Redox", new Switch { RelayNumber = 5, Name = "RedoxSwitch", HighIsOn = false, On = false } },
                    { "Poollampe", new Switch { RelayNumber = 4, Name = "PoolLight", HighIsOn = false, On = false } },
                    { "Three", new Switch { RelayNumber = 3, Name = "Three", HighIsOn = false, On = false } },
                    { "Two", new Switch { RelayNumber = 2, Name = "Two", HighIsOn = false, On = false } },
                    { "One", new Switch { RelayNumber = 1, Name = "One", HighIsOn = false, On = false } }
                },
                FilterPump = new FilterPump { StandardFilterRunTime = 180, StartMorning = new TimeSpan(8, 0, 0), StartNoon = new TimeSpan(14, 0, 0), FilterOff = new TimeSpan(20, 0, 0) },
                SolarHeater = new SolarHeater { SolarHeaterCleaningTime = new TimeSpan(21, 30, 0), SolarHeaterCleaningDuration = 180, TurnOnDiff = 6.0, TurnOffDiff = 3.0, MaxPoolTemp = 29.5 },
                Ph = new Ph { Name = "pHValue", MaxValue = 7.3, AcidInjectionDuration = 20, AcidInjectionRecurringPeriod = 10, IntervalInSec = 60, Address = "99", LedOn = true, ViewFormat = "#0.00", InterfaceFormat = "#0.000", UnitSign = "pH", Value = double.NaN },
                Redox = new Redox { Name = "RedoxValue", On = 750, Off = 840, IntervalInSec = 60, Address = "98", LedOn = true, ViewFormat = "#0", InterfaceFormat = "#0.0", UnitSign = "mV", Value = double.NaN },
                Distance = new Distance { Address = "16/26", Name = "Distance", ViewFormat = "#0.00", InterfaceFormat = "#0.00", UnitSign = "cm", NameL = "Volume", ViewFormatL = "#0", InterfaceFormatL = "#0", UnitSignL = "L", IntervalInSec = 60, Value = double.NaN, NumberOfMeasurements = 5 },
                RelayConfig = RelayConfig.Instance
            };
            if (Data.RelayConfig != null)
                {
                    Data.RelayConfig.RelayToLogicLevelConverterDict = new Dictionary<int, int>();
                    for (var i = 1; i < 9; i++)
                    {
                        Data.RelayConfig.RelayToLogicLevelConverterDict[i] = i;
                    }

                    Data.RelayConfig.LogicLevelConverterToGpioDict = new Dictionary<int, int>
                    {
                        { 1, 19 },
                        { 2, 13 },
                        { 3, 5 },
                        { 4, 22 },
                        { 5, 27 },
                        { 6, 23 },
                        { 7, 20 },
                        { 8, 21 }
                    };
                }

                Data.WinterMode = false;
            }
            else
            {
                Data = Persistence.Instance.Load<PoolData>();
            }
#endif            
        // Set RelayConfig as Singleton
        RelayConfig.Instance = Data?.RelayConfig;

        // Populate Switches
        if (Data != null)
        {
            Data.Switches = new List<Switch>();
            Data.SwitchesObj = new Dictionary<string, object>();
            if (Data.SwitchesDict != null)
            {
                foreach (var (key, swi) in Data.SwitchesDict)
                {
                    swi.Key = key;
                    Data.Switches.Add(swi);
                    Data.SwitchesObj.Add(swi.Key!, swi);
                }

                if (Data.FilterPump != null)
                {
                    Data.FilterPump.Switch = Data.SwitchesDict[Data.FilterPump.GetType().Name];
                    if (Data.SolarHeater != null)
                        Data.SolarHeater.Switch = Data.SwitchesDict[Data.SolarHeater.GetType().Name];
                    if (Data.Redox != null)
                    {
                        Data.Redox.Switch = Data.SwitchesDict[Data.Redox.GetType().Name];
                        Data.Redox.FilterPumpSwitch = Data.SwitchesDict[Data.FilterPump.GetType().Name];
                    }

                    if (Data.Ph != null)
                    {
                        Data.Ph.Switch = Data.SwitchesDict[Data.Ph.GetType().Name];
                        Data.Ph.FilterPumpSwitch = Data.SwitchesDict[Data.FilterPump.GetType().Name];

                        // Populate Temperatures
                        Data.Temperatures = new List<Temperature>();
                        Data.TemperaturesObj = new Dictionary<string, object>();
                        if (Data.TemperaturesDict != null)
                        {
                            foreach (var (key, te) in Data.TemperaturesDict)
                            {
                                te.Key = key;
                                Data.Temperatures.Add(te);
                                Data.TemperaturesObj.Add(te.Key, te);
                            }

                            // Set PoolTemperature for pH correction and Filter Time
                            Data.Ph.PoolTemperature = Data.TemperaturesDict["Pool"];
                        }
                    }

                    // Fire Event for Pool Temperature change for calculating FilterRunTime
                    if (Data.TemperaturesDict != null)
                        Data.TemperaturesDict["Pool"].MeasurementTaken += Data.FilterPump.OnTemperatureChange;
                }
            }

            if (Data.SolarHeater != null)
            {
                if (Data.TemperaturesDict != null)
                {
                    Data.TemperaturesDict["Pool"].MeasurementTaken += Data.SolarHeater.OnTemperatureChange;

                    // Fire Event for SolarHeater Temperature change für calculation SolarHeater Switch On/Off
                    Data.TemperaturesDict["SolarPreRun"].MeasurementTaken += Data.SolarHeater.OnTemperatureChange;
                    Data.TemperaturesDict[Data.SolarHeater.GetType().Name].MeasurementTaken +=
                        Data.SolarHeater.OnTemperatureChange;
                }
            }

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
                sw.SwitchRelay();
            }

            // Open Trigger and Echo Gpio
            Data.OpenGpioEchoAndTrigger();
        }

        // Subscribe events after loading Data, so that loading does not fire events!
        PoolMqttClient.Instance.register(MqttClient_ApplicationMessageReceivedAsync);

        Logger.Information("MainWindowViewModel Initialized");
    }

    // Close Gpio in Destruction
    ~MainWindowViewModel()
    {
        Data?.CloseGpioSwitches();
        Data?.CloseGpioEchoAndTrigger();
    }

    private void Close_Button_Clicked()
    {
        Persistence.Instance.Save(Data);

        Data?.GpioSwitchesOff();
        Data?.CloseGpioSwitches();
        Data?.CloseGpioEchoAndTrigger();
        Gpio.Instance.Dispose();

        PoolMqttClient.Instance.Disconnect();

        Thread.Sleep(3000);

        Logger.Information("-----------------------------------------------------");
        Logger.Information("---------------------- Closing Application ----------");
        Logger.Information("-----------------------------------------------------");

        ((Serilog.Core.Logger)Logger).Dispose();

        //Dispose();

        App.MainWindow?.Close();
    }

    private Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        object? baseObject = Data;
        var objectNameToSet = "";
        var key = "";

        var propertyValue = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment.ToArray());
        var topic = arg.ApplicationMessage.Topic.Replace(PoolControlConfig.Instance.Settings!.BaseTopic.Command, "");

        if (topic.ToLower().Equals("sendstate"))
        {
            PublishMessage("json", Persistence.Instance.Serialize(Data), false);
        }
        else if (topic.ToLower().StartsWith("i2c/"))
        {
            var deviceName = topic.Split("/")[1];
            var i2CObject = (Data!.GetType().GetProperty(deviceName)!.GetValue(Data) as MeasurementModelBase)!.BaseMeasurement;
            MeasurementResult? mr;
            try
            {
                mr  = (MeasurementResult)i2CObject!.GetType().GetMethod("send_i2c_command")!.Invoke(i2CObject, new object[] { propertyValue })!;
            }
            catch (Exception ex)
            {
                mr = new MeasurementResult { Result = 99, ReturnCode = 99, StatusInfo = ex.Message, TimeStamp = DateTime.Now };
            }

            PublishMessage(topic, Persistence.Instance.Serialize(mr), (int)arg.ApplicationMessage.QualityOfServiceLevel, arg.ApplicationMessage.Retain, false);
        }
        else
        {

            try
            {
                string propertyName;
                if (topic.Contains(PropertySplitter))
                {
                    string[] properties = topic.Split(PropertySplitter);

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

                var result = PropertySetter.setProperty(baseObject, propertyName, propertyValue, objectNameToSet, key);

                if (result.Success)
                {
                    if (result.Message != null) Logger.Debug("{Message}", result.Message);
                }
                else
                {
                    if (result.Message != null) Logger.Error("{Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error: topic: {Topic} payload: {Payload}", arg.ApplicationMessage.Topic, arg.ApplicationMessage.PayloadSegment.ToArray());
            }
        }

        return arg.AcknowledgeAsync(new CancellationToken());
    }

    protected override void OnTimerTicked(object? state)
    {
        Persistence.Instance.Save(Data);

        var currentProc = Process.GetCurrentProcess();
        var bytesInUse = currentProc.PrivateMemorySize64 / 1024.0 / 1024.0;

        Logger.Debug("MBytes in use {Bytes} in {Proc}", bytesInUse, currentProc.ProcessName);
    }

    [Reactive][JsonProperty]
    public PoolData? Data { get; set; }
}