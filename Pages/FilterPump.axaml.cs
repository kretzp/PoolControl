using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PoolControl.Pages;

public partial class FilterPump : UserControl
{
    public FilterPump()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}