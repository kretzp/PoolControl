using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PoolControl.Pages;

public partial class Redox : UserControl
{
    public Redox()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}