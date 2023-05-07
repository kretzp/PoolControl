using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PoolControl.Pages;

public partial class PhValue : UserControl
{
    public PhValue()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}