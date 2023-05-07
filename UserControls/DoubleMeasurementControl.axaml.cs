using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PoolControl.UserControls;

public partial class DoubleMeasurementControl : UserControl
{
    public DoubleMeasurementControl()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<DoubleMeasurementControl, string>(nameof(Label));
    public static readonly StyledProperty<string> FirstMeasurementProperty = AvaloniaProperty.Register<DoubleMeasurementControl, string>(nameof(FirstMeasurement));
    public static readonly StyledProperty<string> SecondMeasurementProperty = AvaloniaProperty.Register<DoubleMeasurementControl, string>(nameof(SecondMeasurement));
    public static readonly StyledProperty<IBrush> ButtonBackgroundProperty = AvaloniaProperty.Register<DoubleMeasurementControl, IBrush>(nameof(ButtonBackground));

    public string Label
    {
        get => this.GetValue(LabelProperty);
        set => this.SetValue(LabelProperty, value);
    }

    public string FirstMeasurement
    {
        get => this.GetValue(FirstMeasurementProperty);
        set => this.SetValue(FirstMeasurementProperty, value);
    }

    public string SecondMeasurement
    {
        get => this.GetValue(SecondMeasurementProperty);
        set => this.SetValue(SecondMeasurementProperty, value);
    }

    public IBrush ButtonBackground
    {
        get => this.GetValue(ButtonBackgroundProperty);
        set => this.SetValue(ButtonBackgroundProperty, value);
    }
}