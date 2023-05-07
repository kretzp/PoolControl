using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReactiveUI.Fody.Helpers;

namespace PoolControl.UserControls;

public partial class NumControl : UserControl
{
    public NumControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<NumControl, string>(nameof(Label));
    public static readonly StyledProperty<string> FormatStringProperty = AvaloniaProperty.Register<NumControl, string>(nameof(FormatString), "0");
    public static readonly StyledProperty<double> NumValueProperty = AvaloniaProperty.Register<NumControl, double>(nameof(NumValue));
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<NumControl, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<NumControl, double>(nameof(Maximum));
    public static readonly StyledProperty<double> IncrementProperty = AvaloniaProperty.Register<NumControl, double>(nameof(Increment));
    public static readonly StyledProperty<GridLength> ColumnWidthProperty = AvaloniaProperty.Register<NumControl, GridLength>(nameof(ColumnWidth));
    public static readonly StyledProperty<IBrush> ControlBackgroundProperty = AvaloniaProperty.Register<NumControl, IBrush>(nameof(ControlBackground));

    [Reactive]
    public string Label
    {
        get => this.GetValue(LabelProperty);
        set => this.SetValue(LabelProperty, value);
    }

    [Reactive]
    public string FormatString
    {
        get => this.GetValue(FormatStringProperty);
        set => this.SetValue(FormatStringProperty, value);
    }

    [Reactive]
    public double NumValue
    {
        get => this.GetValue(NumValueProperty);
        set => this.SetValue(NumValueProperty, value);
    }

    [Reactive]
    public double Minimum
    {
        get => this.GetValue(MinimumProperty);
        set => this.SetValue(MinimumProperty, value);
    }

    [Reactive]
    public double Maximum
    {
        get => this.GetValue(MaximumProperty);
        set => this.SetValue(MaximumProperty, value);
    }

    [Reactive]
    public double Increment
    {
        get => this.GetValue(IncrementProperty);
        set => this.SetValue(IncrementProperty, value);
    }

    [Reactive]
    public GridLength ColumnWidth
    {
        get => this.GetValue(ColumnWidthProperty);
        set => this.SetValue(ColumnWidthProperty, value);
    }

    [Reactive]
    public IBrush ControlBackground
    {
        get => this.GetValue(ControlBackgroundProperty);
        set => this.SetValue(ControlBackgroundProperty, value);
    }
}