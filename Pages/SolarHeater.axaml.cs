using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PoolControl.Pages;

public partial class SolarHeater : UserControl
{
    public SolarHeater()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}