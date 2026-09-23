using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PoolControl.Helper;
using PoolControl.ViewModels;
using ReactiveUI;

namespace PoolControl.Views;

public partial class ModernView
{
    private Control? _returnFocus;
    private bool _commandPending;
    private readonly System.Collections.Generic.Stack<(object Content, Control? Focus)> _dialogHistory = new();

    private void OpenDialog(string title, Control body, Action? apply = null, Func<string>? liveTitle = null)
    {
        if (!DialogLayer.IsVisible)
        {
            _dialogHistory.Clear();
            _returnFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        }
        else if (DialogContent.Content is { } previous)
        {
            _dialogHistory.Push((previous, TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control));
        }
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(Button(T("Schließen", "Close"), CloseDialog));
        if (apply != null)
        {
            var accept = Button(T("Übernehmen", "Apply"), apply);
            accept.Name = "ApplyEdit";
            accept.Classes.Add("selected");
            buttons.Children.Add(accept);
        }
        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 16, MaxHeight = 500 };
        content.Children.Add(liveTitle == null ? Text(title, 24) : Live(liveTitle, 24));
        var scroll = new ScrollViewer { Content = body, MaxHeight = 368 };
        Grid.SetRow(scroll, 1); content.Children.Add(scroll);
        Grid.SetRow(buttons, 2); content.Children.Add(buttons);
        KeyboardNavigation.SetTabNavigation(content, KeyboardNavigationMode.Cycle);
        DialogContent.Content = content;
        Shell.IsEnabled = false;
        DialogLayer.IsVisible = true;
        Dispatcher.UIThread.Post(() => buttons.Children.OfType<Button>().First().Focus());
    }

    private void CloseDialog()
    {
        if (_commandPending) return;
        if (_dialogHistory.TryPop(out var previous))
        {
            DialogContent.Content = previous.Content;
            RefreshReadouts();
            Dispatcher.UIThread.Post(() =>
                (previous.Focus ?? DialogContent.GetVisualDescendants().OfType<Button>().FirstOrDefault())?.Focus());
            return;
        }
        DialogLayer.IsVisible = false;
        DialogContent.Content = null;
        Shell.IsEnabled = true;
        var focusName = _returnFocus?.Name;
        Render();
        var focus = focusName == null ? null : this.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == focusName);
        (focus ?? Navigation.Children.OfType<Button>().ElementAt(_page)).Focus();
        _returnFocus = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DialogLayer.IsVisible) { CloseDialog(); e.Handled = true; }
        base.OnKeyDown(e);
    }

    private void Confirm(string title, string explanation, Action action)
    {
        var error = Text("", 14);
        OpenDialog(title, Stack(Text(explanation), error), () =>
        {
            try { action(); CloseDialog(); }
            catch (Exception ex) { error.Text = T("Aktion fehlgeschlagen: ", "Action failed: ") + ex.Message; }
        });
    }

    private Control NumberSetting(object target, string property, string label, string unit, decimal minimum, decimal maximum, decimal step)
    {
        var button = Button("", () => EditNumber(target, property, label, unit, minimum, maximum, step));
        button.Name = "Edit" + property;
        _readouts.Add(() => button.Content = Convert.ToDecimal(target.GetType().GetProperty(property)!.GetValue(target), CultureInfo.InvariantCulture)
            .ToString(step < 1 ? "0.0#" : "0", Culture) + " " + unit);
        return Row(label, button);
    }

    private string EditError(string code) => code == "conflict"
        ? T("Der Wert wurde inzwischen geändert. Schließen und erneut öffnen, um den aktuellen Wert zu bearbeiten.",
            "The value changed externally. Close and reopen to edit the current value.")
        : T("Ungültiger Wert. Bitte Eingabe und zulässigen Bereich prüfen.", "Invalid value. Check the entry and allowed range.");

    private void EditNumber(object target, string property, string label, string unit, decimal minimum, decimal maximum, decimal step)
    {
        var edit = new UiEditSession(target, property);
        var input = new TextBox
        {
            Text = Convert.ToDecimal(edit.Original, CultureInfo.InvariantCulture).ToString(Culture),
            FontSize = 28, MinHeight = 52, HorizontalContentAlignment = HorizontalAlignment.Center,
            Name = "NumericDraft"
        };
        Avalonia.Automation.AutomationProperties.SetName(input, label);
        var error = Text("", 14);
        var entry = new Grid { ColumnDefinitions = new ColumnDefinitions("64,*,64"), ColumnSpacing = 8 };
        void Increment(decimal amount)
        {
            if (decimal.TryParse(input.Text, NumberStyles.Number, Culture, out var value))
            {
                input.Text = Math.Clamp(value + amount, minimum, maximum).ToString(Culture);
                input.SelectionStart = input.SelectionEnd = input.Text.Length;
            }
        }
        var minus = Button("−", () => Increment(-step));
        var plus = Button("+", () => Increment(step));
        entry.Children.Add(minus); Grid.SetColumn(input, 1); entry.Children.Add(input);
        Grid.SetColumn(plus, 2); entry.Children.Add(plus);
        var keys = new UniformGrid { Columns = 3, Rows = 4 };
        var separator = Culture.NumberFormat.NumberDecimalSeparator;
        foreach (var key in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", separator, "0", "⌫" })
        {
            var captured = key;
            var button = Button(key, () =>
            {
                var text = input.Text ?? "";
                var start = Math.Min(input.SelectionStart, input.SelectionEnd);
                var end = Math.Max(input.SelectionStart, input.SelectionEnd);
                if (captured == "⌫" && start == end && start > 0) start--;
                var replacement = captured == "⌫" ? "" : captured;
                input.Text = text[..start] + replacement + text[end..];
                input.SelectionStart = input.SelectionEnd = start + replacement.Length;
            });
            button.Margin = new Thickness(3); button.MinHeight = 52;
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            keys.Children.Add(button);
        }
        var body = Stack(Text($"{minimum.ToString(Culture)} – {maximum.ToString(Culture)} {unit}", 14), entry, keys, error);
        OpenDialog(label, body, () =>
        {
            if (decimal.TryParse(input.Text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture, out var candidate)
                && !ValidThresholds(target, property, candidate))
            {
                error.Text = T("Einschalt- und Ausschaltschwelle passen nicht zusammen.", "Start and stop thresholds are inconsistent.");
                return;
            }
            if (!edit.TryNumber(input.Text ?? "", Culture, minimum, maximum, out var code)) { error.Text = EditError(code); return; }
            CloseDialog();
        });
        Dispatcher.UIThread.Post(() => { input.Focus(); input.SelectAll(); });
    }

    private static bool ValidThresholds(object target, string property, decimal value) => target switch
    {
        Redox redox when property == nameof(Redox.On) => value < redox.Off,
        Redox redox when property == nameof(Redox.Off) => value > redox.On,
        SolarHeater solar when property == nameof(SolarHeater.TurnOnDiff) => (double)value > solar.TurnOffDiff,
        SolarHeater solar when property == nameof(SolarHeater.TurnOffDiff) => (double)value < solar.TurnOnDiff,
        _ => true
    };

    private Control TimeSetting(object target, string property, string label)
    {
        var button = Button("", () => EditTime(target, property, label));
        button.Name = "Edit" + property;
        _readouts.Add(() => button.Content = ((TimeSpan)target.GetType().GetProperty(property)!.GetValue(target)!).ToString(@"hh\:mm"));
        return Row(label, button);
    }

    private void EditTime(object target, string property, string label)
    {
        var edit = new UiEditSession(target, property);
        var time = (TimeSpan)edit.Original!;
        var hour = new NumericUpDown { Value = time.Hours, Minimum = 0, Maximum = 23, Increment = 1, FontSize = 28, MinHeight = 56, FormatString = "00" };
        var minute = new NumericUpDown { Value = time.Minutes, Minimum = 0, Maximum = 59, Increment = 1, FontSize = 28, MinHeight = 56, FormatString = "00" };
        Control TimeColumn(string name, NumericUpDown field, int max) => Stack(Text(name),
            Button("+", () => field.Value = ((field.Value ?? 0) + 1) % (max + 1)), field,
            Button("−", () => field.Value = ((field.Value ?? 0) + max) % (max + 1)));
        var error = Text("", 14);
        OpenDialog(label, Stack(Columns(TimeColumn(T("Stunde", "Hour"), hour, 23), TimeColumn(T("Minute", "Minute"), minute, 59)), error), () =>
        {
            if (hour.Value == null || minute.Value == null || hour.Value != decimal.Truncate(hour.Value.Value)
                || minute.Value != decimal.Truncate(minute.Value.Value) || !edit.TryTime((int)hour.Value, (int)minute.Value, out _))
            { error.Text = EditError(edit.HasConflict ? "conflict" : "value"); return; }
            CloseDialog();
        });
    }

    private void ShowSensors()
    {
        var sensors = Data!.Temperatures?.Cast<MeasurementModelBase>().ToList() ?? new();
        if (Data.Ph != null) sensors.Add(Data.Ph);
        if (Data.Redox != null) sensors.Add(Data.Redox);
        if (Data.Distance != null) sensors.Add(Data.Distance);
        var list = Stack(sensors.Select(sensor =>
        {
            var button = Button("", () => SensorDetails(sensor));
            _readouts.Add(() => button.Content = DisplayName(sensor) + "  ›");
            return button;
        }).ToArray());
        OpenDialog(T("Alle Sensoren", "All sensors"), new ScrollViewer { Content = list, MaxHeight = 350 });
        RefreshReadouts();
    }

    private void ShowRelays()
    {
        var list = Stack((Data!.Switches ?? new()).Select(relay => SwitchRow(relay, DisplayName(relay))).ToArray());
        OpenDialog(T("Relais / Steuerzustände", "Relays / control states"), new ScrollViewer { Content = list, MaxHeight = 350 });
        RefreshReadouts();
    }

    private void SensorDetails(MeasurementModelBase? sensor)
    {
        if (sensor == null) return;
        var list = Stack(Live(() => Reading(sensor) + " · " + Age(sensor), 20),
            SensorTextSetting(sensor, nameof(sensor.Name), T("Name", "Name")),
            AddressSetting(sensor),
            SensorTextSetting(sensor, nameof(sensor.UnitSign), T("Maßeinheit", "Unit")),
            SensorTextSetting(sensor, nameof(sensor.ViewFormat), T("Format UI", "UI format")),
            SensorTextSetting(sensor, nameof(sensor.InterfaceFormat), T("Schnittstellenformat", "Interface format")),
            NumberSetting(sensor, nameof(sensor.IntervalInSec), T("Messintervall", "Sampling interval"), "s", 1, 86400, 5));
        if (sensor is EzoBase ezo)
        {
            list.Children.Add(Live(() => T("Versorgung: ", "Supply: ") + Format(ezo.Voltage, "V") +
                T(" · Kalibrierstatus: ", " · Calibration status: ") + ezo.SensorsCalibrated, 14));
            list.Children.Add(Button(T("Sensor-LED umschalten", "Toggle sensor LED"), () => Confirm("LED", T("Sensor-LED umschalten?", "Toggle sensor LED?"), () => ezo.LedOn = !ezo.LedOn)));
            list.Children.Add(Button(T("Sensor identifizieren", "Identify sensor"), () => RunSensorCommand(T("Identifizieren", "Identify"), ezo.OnFind, sensor)));
            list.Children.Add(Button(T("Kalibrierstatus abfragen", "Read calibration status"), () => RunSensorCommand(T("Kalibrierstatus", "Calibration status"), ezo.OnCalibrated, sensor)));
            if (ezo is Ph ph)
            {
                list.Children.Add(CalibrationPoint(ph, nameof(ph.MidCal), T("Mitte", "Mid"), 5, 8, ph.OnMidCal));
                list.Children.Add(CalibrationPoint(ph, nameof(ph.LowCal), T("Niedrig", "Low"), 3, 5, ph.OnLowCal));
                list.Children.Add(CalibrationPoint(ph, nameof(ph.HighCal), T("Hoch", "High"), 8, 10, ph.OnHighCal));
                list.Children.Add(Button(T("Steigung abfragen", "Read slope"), () => RunSensorCommand(T("Steigung", "Slope"), ph.OnGetSlope, ph)));
                list.Children.Add(Live(() => ph.Slope ?? "—"));
            }
            else if (ezo is Redox redox)
                list.Children.Add(CalibrationPoint(redox, nameof(redox.Cal), "Redox", 0, 1000, redox.OnCal));
            list.Children.Add(Button(T("Kalibrierung löschen", "Clear calibration"), () => RunSensorCommand(T("Kalibrierung löschen", "Clear calibration"), ezo.OnClearCalibration, sensor)));
        }
        OpenDialog(DisplayName(sensor), new ScrollViewer { Content = list, MaxHeight = 350 }, liveTitle: () => DisplayName(sensor));
        RefreshReadouts();
    }

    private Control AddressSetting(MeasurementModelBase sensor)
    {
        var button = Button("", () => EditAddress(sensor));
        button.Name = "EditAddress";
        _readouts.Add(() => button.Content = sensor.Address ?? "—");
        return Row(T("Adresse", "Address"), button);
    }

    private void EditAddress(MeasurementModelBase sensor)
    {
        var edit = new UiEditSession(sensor, nameof(sensor.Address));
        var input = new TextBox { Name = "AddressDraft", Text = sensor.Address ?? "", FontSize = 24, MinHeight = 52 };
        Avalonia.Automation.AutomationProperties.SetName(input, T("Sensoradresse", "Sensor address"));
        var hint = sensor switch
        {
            ViewModels.Temperature => T("1-Wire-ID, z. B. 28-2273e90264ff. Gilt ab der nächsten Messung.", "1-Wire ID, e.g. 28-2273e90264ff. Used for the next measurement."),
            EzoBase => T("I²C-Adresse dezimal (8–119), z. B. 99. Gilt ab dem nächsten Sensorbefehl.", "Decimal I²C address (8–119), e.g. 99. Used for the next sensor command."),
            _ => T("BCM-GPIOs Trigger/Echo, z. B. 16/26. Änderung wird erst nach einem Neustart der Anwendung wirksam.", "BCM GPIOs trigger/echo, e.g. 16/26. Restart the application to use the new pins.")
        };
        var keys = new UniformGrid { Columns = 7, Rows = 3 };
        foreach (var key in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "a", "b", "c", "d", "e", "f", "-", "/", "←", "→", "⌫" })
        {
            var captured = key;
            var button = Button(key, () =>
            {
                var text = input.Text ?? "";
                var start = Math.Min(input.SelectionStart, input.SelectionEnd);
                var end = Math.Max(input.SelectionStart, input.SelectionEnd);
                if (captured is "←" or "→")
                {
                    input.SelectionStart = input.SelectionEnd = Math.Clamp(captured == "←" ? start - 1 : end + 1, 0, text.Length);
                    return;
                }
                if (captured == "⌫" && start == end && start > 0) start--;
                var replacement = captured == "⌫" ? "" : captured;
                input.Text = text[..start] + replacement + text[end..];
                input.SelectionStart = input.SelectionEnd = start + replacement.Length;
            });
            button.Margin = new Thickness(3);
            button.MinHeight = 48;
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            keys.Children.Add(button);
        }
        var error = Text("", 14);
        OpenDialog(T("Sensoradresse", "Sensor address"), Stack(Text(hint, 14), input, keys, error), () =>
        {
            var address = (input.Text ?? "").Trim().ToLowerInvariant();
            if (!ValidAddress(sensor, address)) { error.Text = T("Ungültige Adresse oder GPIO bereits durch ein Relais belegt.", "Invalid address or GPIO already assigned to a relay."); return; }
            if (!edit.TryApply(address, out var code)) { error.Text = EditError(code); return; }
            Persistence.Instance.Save(Data);
            CloseDialog();
        });
        Dispatcher.UIThread.Post(() => { input.Focus(); input.SelectAll(); });
    }

    private bool ValidAddress(MeasurementModelBase sensor, string address)
    {
        if (sensor is Temperature) return Regex.IsMatch(address, @"\A[0-9a-f]{2}-[0-9a-f]{12}\z");
        if (sensor is EzoBase) return int.TryParse(address, NumberStyles.None, CultureInfo.InvariantCulture, out var i2c) && i2c is >= 8 and <= 119;
        if (sensor is not Distance) return false;
        var pins = address.Split('/');
        if (pins.Length != 2 || !int.TryParse(pins[0], NumberStyles.None, CultureInfo.InvariantCulture, out var trigger)
            || !int.TryParse(pins[1], NumberStyles.None, CultureInfo.InvariantCulture, out var echo)
            || trigger is < 1 or > 27 || echo is < 1 or > 27 || trigger == echo) return false;
        return !(Data?.Switches ?? new()).Any(relay => Data?.RelayConfig != null
            && new[] { trigger, echo }.Contains(Data.RelayConfig.GetGpioForRelayNumber(relay.RelayNumber)));
    }

    private Control CalibrationPoint(EzoBase sensor, string property, string label, decimal min, decimal max, ReactiveCommand<Unit, Unit> command)
    {
        return Stack(NumberSetting(sensor, property, label + T(" Referenz", " reference"), sensor.UnitSign ?? "", min, max, sensor is Ph ? .01m : 1),
            Button(label + T(" kalibrieren", " calibrate"), () => RunSensorCommand(label + T(" kalibrieren", " calibrate"), command, sensor)));
    }

    private void RunSensorCommand(string title, ReactiveCommand<Unit, Unit> command, MeasurementModelBase sensor)
    {
        var status = Text(T("Sensor vorbereiten und Referenzwert prüfen. Befehl jetzt ausführen?", "Prepare the sensor and check its reference value. Execute the command now?"));
        OpenDialog(title, status, async () =>
        {
            if (_commandPending) return;
            _commandPending = true;
            DialogContent.IsEnabled = false;
            status.Text = T("Befehl wird ausgeführt …", "Executing command …");
            try
            {
                // Observe both error channels; legacy commands may otherwise reach ReactiveUI's default handler.
                using var errors = command.ThrownExceptions.Subscribe(_ => { });
                await Task.Run(async () => await command.Execute().ToTask());
                status.Text = T("Befehl beendet. Kalibrierstatus am Sensor anschließend abfragen.", "Command completed. Read the sensor's calibration status to verify.");
            }
            catch (Exception ex) { status.Text = T("Sensorbefehl fehlgeschlagen: ", "Sensor command failed: ") + ex.Message; }
            finally { _commandPending = false; DialogContent.IsEnabled = true; }
        });
    }
}
