using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using PoolControl.Helper;
using PoolControl.ViewModels;

namespace PoolControl.Views;

public partial class ModernView
{
    private Control SensorTextSetting(MeasurementModelBase sensor, string property, string label)
    {
        var info = sensor.GetType().GetProperty(property)!;
        var button = Button("", () => EditSensorText(sensor, property, label));
        button.Name = "Edit" + property;
        button.MaxWidth = 320;
        _readouts.Add(() => button.Content = info.GetValue(sensor) as string is { Length: > 0 } value ? value : "—");
        return Row(label, button);
    }

    private void EditSensorText(MeasurementModelBase sensor, string property, string label)
    {
        var edit = new UiEditSession(sensor, property);
        var format = property is nameof(sensor.ViewFormat) or nameof(sensor.InterfaceFormat);
        var mqtt = property == nameof(sensor.InterfaceFormat);
        var input = new TextBox { Name = "SensorTextDraft", Text = edit.Original as string ?? "", FontSize = 24, MinHeight = 48, MaxLength = 64 };
        Avalonia.Automation.AutomationProperties.SetName(input, label);
        var preview = Text("", 14);
        var error = Text("", 14);
        bool Validate(out string example)
        {
            var value = input.Text ?? "";
            example = "";
            if (value.Length > 64 || value.Any(char.IsControl)) return false;
            if (!format) return property != nameof(sensor.Name) || !string.IsNullOrWhiteSpace(value);
            // Bound standard-format precision before calling ToString (e.g. F999999999).
            var standard = Regex.Match(value, @"\A[a-zA-Z]([0-9]+)\z");
            if (standard.Success && (!int.TryParse(standard.Groups[1].Value, out var precision) || precision > 99)) return false;
            try
            {
                var culture = mqtt ? CultureInfo.InvariantCulture : Culture;
                foreach (var sample in new[] { 0, -1234.56789, 1234.56789 })
                    if (sample.ToString(value, culture).Length > 128) return false;
                example = (double.IsFinite(sensor.Value) ? sensor.Value : 12.34567).ToString(value, culture);
                return example.Length <= 128;
            }
            catch (FormatException) { return false; }
        }
        void Preview()
        {
            error.Text = "";
            preview.Text = Validate(out var sample)
                ? format ? T("Vorschau: ", "Preview: ") + sample + (mqtt ? "" : " " + sensor.UnitSign) : ""
                : T("Ungültige Eingabe oder ungültiges Zahlenformat.", "Invalid entry or number format.");
        }
        input.TextChanged += (_, _) => Preview();
        var keys = new UniformGrid { Columns = 10 };
        var symbols = format || property == nameof(sensor.UnitSign);
        var uppercase = false;
        void Keyboard()
        {
            keys.Children.Clear();
            var characters = symbols ? "1234567890#.,-+/%°µ_():;\"'\\" : "qwertzuiopasdfghjklyxcvbnmäöüß";
            var labels = characters.Select(c => uppercase ? c.ToString().ToUpperInvariant() : c.ToString())
                .Concat(new[] { "␣", "←", "→", "⌫", "⇧", symbols ? "ABC" : "123" });
            foreach (var key in labels)
            {
                var captured = key;
                var button = Button(key, () =>
                {
                    if (captured is "ABC" or "123") { symbols = !symbols; Keyboard(); return; }
                    if (captured == "⇧") { uppercase = !uppercase; Keyboard(); return; }
                    var text = input.Text ?? "";
                    var start = Math.Min(input.SelectionStart, input.SelectionEnd);
                    var end = Math.Max(input.SelectionStart, input.SelectionEnd);
                    if (captured is "←" or "→")
                    {
                        input.SelectionStart = input.SelectionEnd = Math.Clamp(captured == "←" ? start - 1 : end + 1, 0, text.Length);
                        return;
                    }
                    if (captured == "⌫" && start == end && start > 0) start--;
                    var replacement = captured == "⌫" ? "" : captured == "␣" ? " " : captured;
                    var candidate = text[..start] + replacement + text[end..];
                    if (candidate.Length > input.MaxLength) return;
                    input.Text = candidate;
                    input.SelectionStart = input.SelectionEnd = start + replacement.Length;
                });
                button.Margin = new Thickness(2);
                button.Padding = new Thickness(4);
                button.MinHeight = 44;
                button.MinWidth = 0;
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                keys.Children.Add(button);
            }
        }
        Keyboard();
        var hint = format ? T("Beispiele: 0.0, 0.00, #0.000. Leer = Standardformat.", "Examples: 0.0, 0.00, #0.000. Empty = default format.")
            : property == nameof(sensor.Name) ? T("Freier Anzeigename oder vorhandener Übersetzungsschlüssel, z. B. Pool.", "Display name or existing translation key, e.g. Pool.")
            : T("Nur die Beschriftung, keine Umrechnung des Messwerts.", "Label only; does not convert the measured value.");
        if (mqtt) hint += T(" MQTT verwendet einen Dezimalpunkt; gilt ab der nächsten Veröffentlichung.", " MQTT uses a decimal point; applies to the next publication.");
        OpenDialog(label, Stack(Text(hint, 14), input, preview, keys, error), () =>
        {
            if (!Validate(out _)) { error.Text = T("Ungültige Eingabe oder ungültiges Zahlenformat.", "Invalid entry or number format."); return; }
            if (!edit.TryApply(input.Text ?? "", out var code)) { error.Text = EditError(code); return; }
            Persistence.Instance.Save(Data);
            CloseDialog();
        });
        Preview();
        Dispatcher.UIThread.Post(() => { input.Focus(); input.SelectAll(); });
    }
}
