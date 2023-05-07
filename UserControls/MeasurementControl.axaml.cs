using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PoolControl.UserControls;

public partial class MeasurementControl : UserControl
{
    public MeasurementControl()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<MeasurementControl, string>(nameof(Label));
    public static readonly StyledProperty<string> MeasurementProperty = AvaloniaProperty.Register<MeasurementControl, string>(nameof(Measurement));
    public static readonly StyledProperty<IBrush> ButtonBackgroundProperty = AvaloniaProperty.Register<MeasurementControl, IBrush>(nameof(ButtonBackground));

    public string Label
    {
        get => this.GetValue(LabelProperty);
        set => this.SetValue(LabelProperty, value);
    }

    public string Measurement
    {
        get => this.GetValue(MeasurementProperty);
        set => this.SetValue(MeasurementProperty, value);
    }

    public IBrush ButtonBackground
    {
        get => this.GetValue(ButtonBackgroundProperty);
        set => this.SetValue(ButtonBackgroundProperty, value);
    }
}