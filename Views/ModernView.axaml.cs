using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PoolControl.Helper;
using PoolControl.ViewModels;

namespace PoolControl.Views;

public partial class ModernView : UserControl
{
    private readonly UiPreferences _preferences;
    private readonly Action _savePreferences;
    private readonly Action _showClassic;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly List<Action> _readouts = new();
    private int _page;
    private bool _redox;
    private PoolData? Data => (DataContext as MainWindowViewModel)?.Data;
    private CultureInfo Culture => CultureInfo.GetCultureInfo(_preferences.Language == "en" ? "en-GB" : "de-DE");
    private string T(string de, string en) => _preferences.Language == "en" ? en : de;

    public ModernView() : this(new UiPreferences(), () => { }, () => { }) { }

    public ModernView(UiPreferences preferences, Action savePreferences, Action showClassic)
    {
        _preferences = preferences;
        _savePreferences = savePreferences;
        _showClassic = showClassic;
        InitializeComponent();
        DataContextChanged += (_, _) => Render();
        AttachedToVisualTree += (_, _) => { Render(); _clock.Start(); };
        DetachedFromVisualTree += (_, _) => _clock.Stop();
        _clock.Tick += (_, _) => RefreshReadouts();
        Render();
    }

    private void ChangeLanguage(object? sender, RoutedEventArgs e)
    {
        _preferences.Language = _preferences.Language == "de" ? "en" : "de";
        _savePreferences();
        Render();
    }

    private void ChangeTheme(object? sender, RoutedEventArgs e)
    {
        _preferences.Theme = _preferences.Theme switch { "System" => "Light", "Light" => "Dark", _ => "System" };
        _savePreferences();
        Render();
    }

    private void ShowClassic(object? sender, RoutedEventArgs e) => _showClassic();

    private void Render()
    {
        ApplyTheme();
        LanguageButton.Content = _preferences.Language == "de" ? "Deutsch" : "English";
        ThemeButton.Content = _preferences.Theme switch { "Light" => T("Hell", "Light"), "Dark" => T("Dunkel", "Dark"), _ => "System" };
        ClassicButton.Content = T("Klassisch", "Classic");
        RenderPage();
    }

    private void ApplyTheme()
    {
        Appearance.RequestedThemeVariant = _preferences.Theme switch
        {
            "Light" => ThemeVariant.Light, "Dark" => ThemeVariant.Dark,
            // App historically requests Dark; explicitly use the platform variant here.
            _ => Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == Avalonia.Platform.PlatformThemeVariant.Light
                ? ThemeVariant.Light : ThemeVariant.Dark
        };
    }

    private void RenderPage()
    {
        _readouts.Clear();
        var names = new[] { T("Übersicht", "Overview"), "Filter", "Solar", T("Wasser", "Water"), T("Zisterne", "Cistern"), "System" };
        var symbols = new[] { "⌂", "≋", "☀", "◇", "▤", "⚙" };
        Navigation.Children.Clear();
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var tab = Button(names[i], () => { _page = index; Render(); });
            tab.Name = "ModernTab" + i;
            tab.Margin = new Thickness(4, 0);
            tab.HorizontalAlignment = HorizontalAlignment.Stretch;
            tab.Content = Stack(Text(symbols[i], 20), Text(names[i], 15));
            Avalonia.Automation.AutomationProperties.SetName(tab, names[i]);
            if (_page == i) tab.Classes.Add("selected");
            Navigation.Children.Add(tab);
        }
        PageTitle.Text = names[_page];
        PageSubtitle.Text = new[]
        {
            T("Dein Pool auf einen Blick", "Your pool at a glance"),
            T("Laufzeiten und manuelle Bedienung", "Schedule and manual operation"),
            T("Sonnenwärme effizient nutzen", "Make the most of solar heat"),
            T("Messwerte und Wasserpflege", "Readings and water treatment"),
            T("Regenwasser im Blick", "Rainwater at a glance"),
            T("Anzeige, Betrieb und Wartung", "Appearance, operation and service")
        }[_page];
        PageContent.Content = Data == null ? Text(T("Keine Anlagenkonfiguration geladen.", "No equipment configuration loaded.")) :
            _page switch { 0 => Overview(), 1 => Filter(), 2 => Solar(), 3 => Water(), 4 => Cistern(), _ => SystemPage() };
        RefreshReadouts();
    }

    private void RefreshReadouts()
    {
        ApplyTheme();
        Clock.Text = DateTime.Now.ToString("HH:mm", Culture);
        foreach (var refresh in _readouts.ToArray()) refresh();
    }

    private string DisplayName(ViewModelBase model) => Resource.ResourceManager.GetString(model.Name, Culture) ?? model.Name;
    private string Format(double value, string unit = "", string format = "0.0")
    {
        if (!double.IsFinite(value)) return "—";
        try { return value.ToString(format, Culture) + (unit.Length > 0 ? " " + unit : ""); }
        catch (FormatException) { return value.ToString("0.0", Culture) + " " + unit; }
    }
    private string Reading(MeasurementModelBase? model) => model == null ? "—" : Format(model.Value, model.UnitSign ?? "", model.ViewFormat ?? "0.0");
    private string Age(MeasurementModelBase? model)
    {
        if (model == null || model.TimeStamp == default || !double.IsFinite(model.Value)) return T("Kein gültiger Messwert", "No valid reading");
        var now = DateTime.Now;
        var seconds = Math.Max(0, (now - model.TimeStamp).TotalSeconds);
        var age = MeasurementAge.Format(model.TimeStamp, now, Culture);
        return seconds > Math.Max(5, model.IntervalInSec * 3)
            ? T("Veraltet · ", "Stale · ") + age : T("Messung vor ", "Reading ") + age + T("", " ago");
    }

    private Temperature? Temperature(string key) => Data?.TemperaturesDict?.GetValueOrDefault(key);
    private static StackPanel Stack(params Control[] controls)
    {
        var panel = new StackPanel { Spacing = 8 };
        foreach (var control in controls) panel.Children.Add(control);
        return panel;
    }
    private static TextBlock Text(string text, double size = 17) => new()
    {
        Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center
    };
    private TextBlock Live(Func<string> read, double size = 17)
    {
        var text = Text(read(), size);
        _readouts.Add(() => text.Text = read());
        return text;
    }
    private static Button Button(string label, Action action)
    {
        var button = new Button { Content = label };
        button.Classes.Add("modern");
        button.Click += (_, _) => action();
        return button;
    }
    private static Control Columns(Control left, Control right)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 16 };
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }
    private static Border Card(Control child, bool tinted = false)
    {
        var border = new Border { Child = child, Padding = new Thickness(20), CornerRadius = new CornerRadius(18) };
        border.Bind(Border.BackgroundProperty, new DynamicResourceExtension(tinted ? "ModernSoft" : "ModernSurface"));
        return border;
    }
    private static ScrollViewer Scroll(Control child) => new() { Content = child, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private Control Row(string label, Control value) => Row(Text(label), value);

    private Control Row(Control label, Control value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12, MinHeight = 52 };
        grid.Children.Add(label);
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
        return grid;
    }
    private Control Metric(string label, MeasurementModelBase? model, Action? action = null, double size = 48)
    {
        var age = Live(() => Age(model), 14); age.Classes.Add("muted");
        var contents = Stack(Live(() => model == null ? label : DisplayName(model)), Live(() => Reading(model), size), age);
        if (action == null) return contents;
        var button = Button(label, action); button.Content = contents;
        if (size >= 48)
        {
            button.Bind(BackgroundProperty, new DynamicResourceExtension("ModernSoft"));
            button.BorderThickness = new Thickness(0);
            button.Padding = new Thickness(0);
        }
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        return button;
    }
    private Control SwitchRow(Switch? relay, string label)
    {
        if (relay == null) return Row(label, Text(T("Nicht eingerichtet", "Not configured"), 14));
        var button = Button("", () =>
        {
            var original = relay.On;
            Confirm(label + (original ? T(" ausschalten", " off") : T(" einschalten", " on")),
                T("Relais manuell schalten? Die Automatik kann diesen Zustand wieder ändern. Angezeigt wird der Steuerzustand, keine elektrische Rückmeldung.",
                  "Switch the relay manually? Automatic control may change it again. This is the control state, not electrical feedback."), () =>
                {
                    if (relay.On != original) throw new InvalidOperationException(EditError("conflict"));
                    relay.On = !original;
                });
        });
        _readouts.Add(() =>
        {
            button.Content = relay.On ? T("Ein", "On") : T("Aus", "Off");
            button.Classes.Set("selected", relay.On);
        });
        return Row(label, button);
    }

    private Control Overview()
    {
        var pool = Temperature("Pool");
        var lamp = Data!.Switches?.FirstOrDefault(relay => relay.Name == "PoolLight")
            ?? Data.SwitchesDict?.GetValueOrDefault("PoolLight")
            ?? Data.SwitchesDict?.GetValueOrDefault("Poollampe");
        var hero = Stack(Metric(T("Pooltemperatur", "Pool temperature"), pool, () => SensorDetails(pool)),
            Row(Live(() => Temperature("SolarHeater") is { } sensor ? DisplayName(sensor) : T("Kollektor", "Collector")), Live(() => Reading(Temperature("SolarHeater")))),
            Row(Live(() => Temperature("SolarPreRun") is { } sensor ? DisplayName(sensor) : T("Solar-Vorlauf", "Solar supply")), Live(() => Reading(Temperature("SolarPreRun")))),
            Row(T("Zisterne", "Cistern"), Live(() => Data!.Distance == null ? "—" : Format(Data.Distance.ValueL, "l", "0"))));
        var ph = Metric("pH", Data!.Ph, () => { _page = 3; _redox = false; Render(); }, 30);
        var redox = Metric("Redox", Data.Redox, () => { _page = 3; _redox = true; Render(); }, 30);
        var right = Stack(Columns(ph, redox),
            SwitchRow(Data.FilterPump?.Switch, T("Filterpumpe", "Filter pump")),
            SwitchRow(Data.SolarHeater?.Switch, T("Solarpumpe", "Solar pump")),
            SwitchRow(lamp, T("Poollampe", "Pool light")));
        return Columns(Card(hero, true), Card(right));
    }

    private Control Filter()
    {
        var pump = Data!.FilterPump;
        if (pump == null) return Text(T("Filter nicht eingerichtet", "Filter not configured"));
        return Columns(Card(Stack(Metric(T("Pooltemperatur", "Pool temperature"), Temperature("Pool")),
            SwitchRow(pump.Switch, T("Filterpumpe", "Filter pump")),
            Live(() => T("Nächster Start: ", "Next start: ") + pump.NextStart?.ToString("dd.MM. HH:mm", Culture), 14),
            Live(() => T("Nächstes Ende: ", "Next end: ") + pump.NextEnd?.ToString("dd.MM. HH:mm", Culture), 14)), true),
            Card(Stack(TimeSetting(pump, nameof(pump.StartMorning), T("Start morgens", "Morning start")),
                TimeSetting(pump, nameof(pump.StartNoon), T("Start nachmittags", "Afternoon start")),
                TimeSetting(pump, nameof(pump.FilterOff), T("Spätestens aus", "Latest stop")),
                NumberSetting(pump, nameof(pump.StandardFilterRunTime), T("Basislaufzeit", "Base runtime"), "min", 1, 1440, 10))));
    }

    private Control Solar()
    {
        var solar = Data!.SolarHeater;
        if (solar == null) return Text(T("Solar nicht eingerichtet", "Solar not configured"));
        return Columns(Card(Stack(Metric(T("Kollektor", "Collector"), Temperature("SolarHeater")),
            SwitchRow(solar.Switch, T("Solarheizung", "Solar heating")),
            TimeSetting(solar, nameof(solar.SolarHeaterCleaningTime), T("Spülung täglich", "Daily flush")),
            NumberSetting(solar, nameof(solar.SolarHeaterCleaningDuration), T("Spüldauer", "Flush duration"), "s", 1, 86400, 10)), true),
            Card(Stack(Text(T("Temperaturregelung", "Temperature control"), 21),
                NumberSetting(solar, nameof(solar.MaxPoolTemp), T("Pool maximal", "Pool maximum"), "°C", 10, 32, .1m),
                NumberSetting(solar, nameof(solar.TurnOnDiff), T("Einschaltdifferenz", "Start difference"), "°C", 0, 10, .1m),
                NumberSetting(solar, nameof(solar.TurnOffDiff), T("Ausschaltdifferenz", "Stop difference"), "°C", 0, 10, .1m),
                Live(() => T("Pool: ", "Pool: ") + Reading(Temperature("Pool")), 14))));
    }

    private Control Water()
    {
        EzoBase? sensor = _redox ? Data!.Redox : Data!.Ph;
        if (sensor == null) return Text(T("Sensor nicht eingerichtet", "Sensor not configured"));
        var phTab = Button("pH", () => { _redox = false; Render(); });
        var redoxTab = Button("Redox", () => { _redox = true; Render(); });
        phTab.Classes.Set("selected", !_redox); redoxTab.Classes.Set("selected", _redox);
        phTab.HorizontalAlignment = redoxTab.HorizontalAlignment = HorizontalAlignment.Stretch;
        var tabs = Columns(phTab, redoxTab);
        var left = Stack(tabs, Metric(_redox ? "Redox" : "pH", sensor, null, 42),
            NumberSetting(sensor, nameof(sensor.IntervalInSec), T("Messintervall", "Sampling interval"), "s", 1, 86400, 5),
            Button(T("Sensor & Kalibrierung", "Sensor & calibration"), () => SensorDetails(sensor)));
        var right = Stack(Text(_redox ? T("Salzelektrolyse", "Salt electrolysis") : T("pH-Dosierung", "pH dosing"), 21));
        if (sensor is Ph ph)
        {
            right.Children.Add(NumberSetting(ph, nameof(ph.MaxValue), T("pH-Obergrenze", "pH upper limit"), "pH", 7, 7.5m, .1m));
            right.Children.Add(NumberSetting(ph, nameof(ph.AcidInjectionRecurringPeriod), T("Wiederholintervall", "Repeat interval"), "min", 1, 1440, 1));
            right.Children.Add(NumberSetting(ph, nameof(ph.AcidInjectionDuration), T("Dosierdauer", "Dosing duration"), "s", 1, 3600, 1));
        }
        else if (sensor is Redox redox)
        {
            right.Children.Add(NumberSetting(redox, nameof(redox.On), T("Einschalten unter", "Start below"), "mV", 100, 1000, 10));
            right.Children.Add(NumberSetting(redox, nameof(redox.Off), T("Ausschalten über", "Stop above"), "mV", 100, 1000, 10));
        }
        right.Children.Add(SwitchRow(sensor.Switch, T("Relais manuell", "Manual relay")));
        return Columns(Card(left), Card(right));
    }

    private Control Cistern()
    {
        var distance = Data!.Distance;
        if (distance == null) return Text(T("Zisterne nicht eingerichtet", "Cistern not configured"));
        return Columns(Card(Stack(Text(T("Wasservorrat", "Water reserve"), 21),
            Live(() => Format(distance.ValueL, "l", "0"), 60), Live(() => Age(distance), 14),
            Text(T("Volumen aus der konfigurierten Abstandsmessung.", "Volume from the configured distance measurement."), 16)), true),
            Card(Stack(Metric(T("Sensorabstand", "Sensor distance"), distance),
                NumberSetting(distance, nameof(distance.NumberOfMeasurements), T("Messungen pro Zyklus", "Samples per cycle"), "", 1, 100, 1),
                Button(T("Sensordetails", "Sensor details"), () => SensorDetails(distance)))));
    }

    private Control SystemPage()
    {
        return Columns(Card(Stack(Text(T("Anzeige & Sprache", "Display & language"), 21),
            Button(T("Sprache: Deutsch", "Language: English"), () => ChangeLanguage(null, new RoutedEventArgs())),
            Button(T("Theme ändern", "Change theme"), () => ChangeTheme(null, new RoutedEventArgs())),
            Button(T("Klassische Oberfläche", "Classic interface"), _showClassic),
            Button(T("Alle Sensoren", "All sensors"), ShowSensors))),
            Card(Stack(Text(T("Betrieb & Wartung", "Operation & service"), 21),
                Button(T("Relais / Ausgänge", "Relays / outputs"), ShowRelays),
                Button(T("Winterbetrieb", "Winter mode"), () => Confirm(T("Winterbetrieb", "Winter mode"),
                    T("Modus wechseln? Beim Wechsel werden alle Relais ausgeschaltet. Im Winterbetrieb pausieren die automatischen Poolfunktionen.",
                      "Change mode? Switching modes turns all relays off. Winter mode pauses automatic pool functions."), () => Data!.WinterMode = !Data.WinterMode)),
                Live(() => Data!.WinterMode ? T("Winterbetrieb aktiv", "Winter mode active") : T("Normalbetrieb", "Normal operation"), 14),
                Button(T("Anwendung beenden", "Exit application"), () => Confirm(T("Anwendung beenden", "Exit application"),
                    T("Die lokale Steuerung wird beendet und die Relais werden ausgeschaltet.", "Local control will stop and relays will be switched off."), () => App.MainWindow?.Close())))));
    }
}
