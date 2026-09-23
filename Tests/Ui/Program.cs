using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MQTTnet.Client;
using PoolControl.Communication;
using PoolControl.Helper;
using PoolControl.ViewModels;
using PoolControl.Views;

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var testDirectory = Path.Combine(AppContext.BaseDirectory, "ui-test-data");
var ageStart = new DateTime(2024, 2, 29, 12, 0, 0);
var germanAge = CultureInfo.GetCultureInfo("de-DE");
Assert(MeasurementAge.Format(ageStart, ageStart.AddSeconds(59), germanAge) == "59 s", "Seconds age");
Assert(MeasurementAge.Format(ageStart, ageStart.AddSeconds(60), germanAge) == "1 min", "Minute boundary");
Assert(MeasurementAge.Format(ageStart, ageStart.AddHours(1), germanAge) == "1 h", "Hour boundary");
Assert(MeasurementAge.Format(ageStart, ageStart.AddDays(1), germanAge) == "1 T", "Day boundary");
Assert(MeasurementAge.Format(ageStart, ageStart.AddYears(1), germanAge) == "1 J", "Leap-year anniversary");
var ageEnd = ageStart.AddYears(2).AddDays(3).AddHours(4).AddMinutes(5).AddSeconds(6);
Assert(MeasurementAge.Format(ageStart, ageEnd, germanAge) == "2 J 3 T 4 h 5 min 6 s", "Full age");
Assert(MeasurementAge.Format(ageStart, ageEnd, CultureInfo.GetCultureInfo("en-GB")) == "2 yr 3 d 4 h 5 min 6 s", "English age");
Assert(MeasurementAge.Format(ageStart, ageStart.AddSeconds(-1), germanAge) == "0 s", "Future timestamp");
Console.WriteLine("PASS measurement age units, boundaries, leap year and future timestamp");
Directory.CreateDirectory(testDirectory);
// Never load production credentials or start the hardware lifecycle in these tests.
File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), """
{"Serilog":{"MinimumLevel":"Fatal"},"Settings":{"PersistenceFile":"ui-fixture.json","PersistenceSaveIntervalInSec":60,
"MQTT":{"Server":"127.0.0.1","Port":1},"BaseTopic":{"Command":"ui-test/cmd/","State":"ui-test/state/"},
"LWT":{"Topic":"ui-test/LWT","ConnectMessage":"test","DisconnectMessage":"test"}}}
""");
Directory.SetCurrentDirectory(testDirectory);
File.WriteAllText("winui-fixture.json", "{}");
File.WriteAllText("ui-fixture.json", "{}");

var path = Path.Combine(testDirectory, "ui.json");
var prefs = new UiPreferences { Surface = "Classic", Language = "en", Theme = "Light" };
prefs.Save(path);
var loaded = UiPreferences.Load(path);
Assert(loaded.Surface == "Classic" && loaded.Language == "en" && loaded.Theme == "Light", "Preference roundtrip");
File.WriteAllText(path, "invalid json");
Assert(UiPreferences.Load(path).Surface == "Modern", "Corrupt preferences fallback");
File.WriteAllText(path, "{\"Surface\":\"unknown\",\"Language\":\"xx\",\"Theme\":\"bad\"}");
Assert(UiPreferences.Load(path).Language == "de", "Unknown preferences fallback");
var edited = new EditTarget();
var draft = new UiEditSession(edited, nameof(edited.Value));
Assert(draft.TryNumber("7,3", CultureInfo.GetCultureInfo("de-DE"), 7, 7.5m, out _) && edited.Value == 7.3, "German number");
draft = new UiEditSession(edited, nameof(edited.Value));
Assert(!draft.TryNumber("NaN", CultureInfo.InvariantCulture, 7, 7.5m, out _), "Reject NaN");
Assert(!draft.TryNumber("9", CultureInfo.InvariantCulture, 7, 7.5m, out _) && edited.Value == 7.3, "Range protects model");
edited.Value = 7.4;
Assert(!draft.TryNumber("7.2", CultureInfo.InvariantCulture, 7, 7.5m, out var code) && code == "conflict" && edited.Value == 7.4, "External update conflict");
var timeEdit = new UiEditSession(edited, nameof(edited.Start));
Assert(!timeEdit.TryTime(24, 0, out _), "Reject 24:00");
Assert(timeEdit.TryTime(8, 30, out _) && edited.Start == new TimeSpan(8, 30, 0), "Time edit");
Console.WriteLine("PASS preferences, localized numbers, validation and edit conflicts");

AppBuilder.Configure<PoolControl.App>().UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
var vm = new MainWindowViewModel(new SilentMqtt());
var pool = new Temperature { Name = "Pool", Value = 26.4, UnitSign = "°C", ViewFormat = "0.0", IntervalInSec = 30, TimeStamp = DateTime.Now };
var collector = new Temperature { Name = "SolarHeater", Value = 34.8, UnitSign = "°C", ViewFormat = "0.0", IntervalInSec = 30, TimeStamp = DateTime.Now };
var relay = new Switch { Name = "FilterPump", Key = "FilterPump", On = true };
var solarRelay = new Switch { Name = "SolarHeater", Key = "SolarHeater" };
var phRelay = new Switch { Name = "PhPump", Key = "Ph" };
var redoxRelay = new Switch { Name = "RedoxSwitch", Key = "Redox" };
var lamp = new Switch { Name = "PoolLight", Key = "PoolLight" };
vm.Data = new PoolData
{
    Temperatures = new() { pool, collector, pool, pool, pool },
    TemperaturesDict = new() { ["Pool"] = pool, ["SolarHeater"] = collector, ["SolarPreRun"] = pool },
    Switches = new() { relay, solarRelay, phRelay, redoxRelay, lamp },
    SwitchesDict = new() { ["FilterPump"] = relay, ["SolarHeater"] = solarRelay, ["Ph"] = phRelay, ["Redox"] = redoxRelay, ["PoolLight"] = lamp },
    FilterPump = new FilterPump { Switch = relay, StandardFilterRunTime = 120, StartMorning = TimeSpan.FromHours(8), StartNoon = TimeSpan.FromHours(14), FilterOff = TimeSpan.FromHours(20) },
    SolarHeater = new SolarHeater { Switch = solarRelay, MaxPoolTemp = 28, TurnOnDiff = 4, TurnOffDiff = 2, SolarHeaterCleaningDuration = 60, SolarHeaterCleaningTime = TimeSpan.FromHours(12) },
    Ph = new Ph { Switch = phRelay, Value = 7.2, UnitSign = "pH", ViewFormat = "0.0", Name = "pHValue", TimeStamp = DateTime.Now, IntervalInSec = 30, MaxValue = 7.3, AcidInjectionDuration = 10, AcidInjectionRecurringPeriod = 15 },
    Redox = new Redox { Switch = redoxRelay, Value = 720, UnitSign = "mV", ViewFormat = "0", Name = "RedoxValue", TimeStamp = DateTime.Now, IntervalInSec = 30, On = 650, Off = 750 },
    Distance = new Distance { Value = 40, UnitSign = "cm", ViewFormat = "0.0", Name = "Distance", TimeStamp = DateTime.Now, IntervalInSec = 30, NumberOfMeasurements = 5 }
};
prefs = new UiPreferences { Theme = "Light" };
var host = new SurfaceHost(prefs, path) { DataContext = vm };
var window = new Window { Width = 1024, Height = 600, Content = host, WindowDecorations = WindowDecorations.None };
window.Show();
void Flush() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
void Click(string name)
{
    var button = window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);
    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Flush();
}
void Snapshot(string name)
{
    Flush();
    using var frame = window.CaptureRenderedFrame();
    Assert(frame != null, "No rendered frame");
    frame!.Save(Path.Combine(testDirectory, name + ".png"));
}
void ClickText(string text)
{
    window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == text)
        .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Flush();
}
string? DialogTitle() => window.GetVisualDescendants().OfType<ModernView>().Single()
    .FindControl<ContentControl>("DialogContent")!.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text;
Flush();
for (var language = 0; language < 2; language++)
{
    for (var theme = 0; theme < 3; theme++)
    {
        for (var page = 0; page < 6; page++)
        {
            Click("ModernTab" + page);
            var view = window.GetVisualDescendants().OfType<ModernView>().Single();
            var panel = view.FindControl<ContentControl>("PageContent")!;
            foreach (var button in panel.GetVisualDescendants().OfType<Button>())
            {
                var position = button.TranslatePoint(default, window)!.Value;
                Assert(position.Y + button.Bounds.Height <= 521, $"Control overlaps navigation: page {page}, {button.Name}, bottom {position.Y + button.Bounds.Height}");
                Assert(position.X >= 0 && position.X + button.Bounds.Width <= 1024, $"Horizontal overflow: {button.Name}");
            }
            Snapshot($"{prefs.Language}-{prefs.Theme}-{page}");
        }
        Click("ThemeButton");
    }
    Click("LanguageButton");
}
Console.WriteLine("PASS 36 page/language/theme layouts at 1024x600");
var originalPhTime = vm.Data.Ph.TimeStamp;
var originalRedoxTime = vm.Data.Redox.TimeStamp;
vm.Data.Ph.TimeStamp = vm.Data.Redox.TimeStamp = DateTime.Now.AddYears(-2).AddDays(-3).AddHours(-4).AddMinutes(-5).AddSeconds(-6);
Click("ModernTab0");
Snapshot("overview-old-measurements");
vm.Data.Ph.TimeStamp = originalPhTime;
vm.Data.Redox.TimeStamp = originalRedoxTime;

Click("ModernTab0");
Button LampButton() => window.GetVisualDescendants().OfType<Grid>()
    .Single(g => g.Children.OfType<TextBlock>().Any(t => t.Text == "Poollampe"))
    .Children.OfType<Button>().Single();
Assert((string?)LampButton().Content == "Aus", "PoolLight relay not resolved on overview");
lamp.On = true;
Click("ModernTab0");
Assert((string?)LampButton().Content == "Ein", "Overview does not show the lamp relay state");
lamp.On = false;
vm.Data.SwitchesDict.Remove("PoolLight");
vm.Data.SwitchesDict["Poollampe"] = lamp;
Click("ModernTab0");
Assert((string?)LampButton().Content == "Aus", "Legacy lamp configuration not resolved");
Button SolarButton() => window.GetVisualDescendants().OfType<Grid>()
    .Single(g => g.Children.OfType<TextBlock>().Any(t => t.Text == "Solarpumpe"))
    .Children.OfType<Button>().Single();
Assert((string?)SolarButton().Content == "Aus", "Solar off state missing");
solarRelay.On = true;
Click("ModernTab0");
Assert((string?)SolarButton().Content == "Ein" && SolarButton().Classes.Contains("selected"), "Solar on state missing");
Snapshot("overview-solar-on");
solarRelay.On = false;
var poolButton = window.GetVisualDescendants().OfType<Button>()
    .Single(b => b.Content is StackPanel s && s.Children.OfType<TextBlock>().Any(t => t.Text == "Pool"));
poolButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Flush();
Click("EditAddress");
TextBox AddressInput() => window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "AddressDraft");
AddressInput().Text = "../../bad";
Click("ApplyEdit");
Assert(pool.Address == null, "Invalid address accepted");
AddressInput().Text = "28-012345abcdef";
Snapshot("address-editor");
Click("ApplyEdit");
Assert(pool.Address == "28-012345abcdef", "Address apply failed");
Assert(File.ReadAllText("winui-fixture.json").Contains("28-012345abcdef"), "Address not persisted");
Click("EditAddress");
AddressInput().Text = "28-111111111111";
pool.Address = "28-222222222222";
Click("ApplyEdit");
Assert(pool.Address == "28-222222222222", "Address edit overwrote external update");
window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Schließen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Flush();
Console.WriteLine("PASS solar states, sensor address validation, apply, persistence and conflict protection");
Assert(DialogTitle() == "Pool", "Closing address editor must return to sensor details");
TextBox SensorTextInput() => window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "SensorTextDraft");
void EditSensorText(string property, string value)
{
    Click("Edit" + property);
    SensorTextInput().Text = value;
    Click("ApplyEdit");
}
EditSensorText("Name", "Mein Becken");
Assert(pool.Name == "Mein Becken" && pool.LocationName == "Mein Becken" && DialogTitle() == "Mein Becken", "Custom sensor name not applied to both surfaces");
EditSensorText("UnitSign", "Grad");
EditSensorText("ViewFormat", "0.000");
Assert(window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("26,400 Grad") == true), "UI format or unit not applied");
Click("EditInterfaceFormat");
SensorTextInput().Text = "D2";
Click("ApplyEdit");
Assert(pool.InterfaceFormat == null, "Invalid double format accepted");
SensorTextInput().Text = "F999999999";
Click("ApplyEdit");
Assert(pool.InterfaceFormat == null, "Unbounded precision accepted");
SensorTextInput().Text = "0.00";
Snapshot("sensor-interface-format-editor");
Click("ApplyEdit");
Assert(pool.InterfaceFormatDecimalPoint == "26.40", "MQTT formatter must use configured format and decimal point");
var persistedSensor = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("winui-fixture.json"))["Temperatures"]!["Pool"]!;
Assert((string?)persistedSensor["Name"] == "Mein Becken" && (string?)persistedSensor["UnitSign"] == "Grad"
    && (string?)persistedSensor["ViewFormat"] == "0.000" && (string?)persistedSensor["InterfaceFormat"] == "0.00", "Sensor text settings not persisted");
var reloadedSensor = Newtonsoft.Json.JsonConvert.DeserializeObject<PoolData>(File.ReadAllText("winui-fixture.json"))!.TemperaturesDict!["Pool"];
Assert(reloadedSensor.LocationName == "Mein Becken" && reloadedSensor.UnitSign == "Grad"
    && reloadedSensor.Value.ToString(reloadedSensor.ViewFormat, germanAge) == "26,400"
    && reloadedSensor.InterfaceFormatDecimalPoint == "26.40", "Sensor text settings not effective after reload");
Click("EditName");
SensorTextInput().Text = "Entwurf";
pool.Name = "Extern geändert";
Click("ApplyEdit");
Assert(pool.Name == "Extern geändert", "Text editor overwrote external update");
ClickText("Schließen");
EditSensorText("Name", "Pool");
Console.WriteLine("PASS sensor name, unit, UI/interface formats, validation, persistence and conflict handling");
ClickText("Schließen");

Click("ModernTab5");
ClickText("Alle Sensoren");
ClickText("Solarheizung  ›");
Assert(DialogTitle() != "Alle Sensoren", "Sensor details did not open");
Click("EditIntervalInSec");
window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "NumericDraft").Text = "45";
Click("ApplyEdit");
Assert(collector.IntervalInSec == 45 && DialogTitle() != "Alle Sensoren", "Applying must return to sensor details");
ClickText("Schließen");
Assert(DialogTitle() == "Alle Sensoren", "Closing sensor details must return to sensor list");
ClickText("Redox-Wert  ›");
window.KeyPress(Avalonia.Input.Key.Escape, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.Escape, null);
window.KeyRelease(Avalonia.Input.Key.Escape, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.Escape, null);
Flush();
Assert(DialogTitle() == "Alle Sensoren", "Second sensor must return to sensor list");
ClickText("Schließen");
Assert(window.GetVisualDescendants().OfType<ModernView>().Single().FindControl<Grid>("Shell")!.IsEnabled, "Closing list must return to System");
ClickText("Relais / Ausgänge");
void OpenLampAction()
{
    var content = window.GetVisualDescendants().OfType<ModernView>().Single().FindControl<ContentControl>("DialogContent")!;
    content.GetVisualDescendants().OfType<Grid>()
        .Single(g => g.Children.OfType<TextBlock>().Any(t => t.Text == "Poollampe"))
        .Children.OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Flush();
}
OpenLampAction();
ClickText("Schließen");
Assert(DialogTitle() == "Relais / Steuerzustände" && !lamp.On, "Cancel relay action must return to list without switching");
OpenLampAction();
Click("ApplyEdit");
Assert(DialogTitle() == "Relais / Steuerzustände" && lamp.On, "Apply relay action must return to list");
Snapshot("relay-list-after-apply");
lamp.On = false;
ClickText("Schließen");
Assert(window.GetVisualDescendants().OfType<ModernView>().Single().FindControl<Grid>("Shell")!.IsEnabled, "Relay list close must return to System");
Console.WriteLine("PASS nested sensor and relay navigation after close and apply");

Click("ModernTab1");
Click("EditStandardFilterRunTime");
var input = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "NumericDraft");
input.Text = "130";
Assert(vm.Data.FilterPump.StandardFilterRunTime == 120, "Draft changed live model");
Snapshot("numeric-editor");
Click("ApplyEdit");
Assert(vm.Data.FilterPump.StandardFilterRunTime == 130, "Apply failed");
Click("EditStandardFilterRunTime");
input = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "NumericDraft");
input.Text = "140";
vm.Data.FilterPump.StandardFilterRunTime = 150;
Click("ApplyEdit");
Assert(vm.Data.FilterPump.StandardFilterRunTime == 150, "UI overwrote remote update");
window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Schließen").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Flush();
for (var i = 0; i < 4; i++)
{
    Click("ClassicButton");
    var classic = window.GetVisualDescendants().OfType<MainView>().Single();
    Assert(ReferenceEquals(classic.DataContext, vm), "Classic model replaced");
    if (i == 0) Snapshot("classic-switch");
    Click("SwitchToModern");
    var modern = window.GetVisualDescendants().OfType<ModernView>().Single();
    Assert(ReferenceEquals(modern.DataContext, vm), "Modern model replaced");
    Assert(vm.Data.FilterPump.StandardFilterRunTime == 150, "Settings lost on switch");
}
Assert(UiPreferences.Load(path).Surface == "Modern", "Surface not persisted");
Console.WriteLine("PASS touch edit/apply, external-update protection, repeated surface switch and shared view model");
window.Close();
PoolMqttClient.Instance.Dispose();
Console.WriteLine("Screenshots: " + testDirectory);

sealed class EditTarget
{
    [Range(7, 7.5)] public double Value { get; set; } = 7.2;
    public TimeSpan Start { get; set; }
}
sealed class SilentMqtt : IPoolMqttClient
{
    public void Dispose() { }
    public Task PublishAsync(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishMessage(string topic, string? payload, int qos, bool retain, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SubscribeAsync(string topic, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnsureConnectedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Register(Func<MqttApplicationMessageReceivedEventArgs, Task> handler) { }
    public void UnRegister(Func<MqttApplicationMessageReceivedEventArgs, Task> handler) { }
}
