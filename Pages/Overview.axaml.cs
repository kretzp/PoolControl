using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PoolControl.Pages;

public partial class Overview : UserControl
{
    public Overview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var temperatureConfig = new TemperatureConfig();
        var temperature = ((Button)sender).Tag;
        temperatureConfig.DataContext = temperature;
        temperatureConfig.ShowDialog(App.MainWindow);
    }

    private void OnButtonClickPh(object sender, RoutedEventArgs e)
    {
        var pHConfig = new PhConfig();
        var temperature = ((Button)sender).Tag;
        pHConfig.DataContext = temperature;
        pHConfig.ShowDialog(App.MainWindow);
    }

    private void OnButtonClickRedox(object sender, RoutedEventArgs e)
    {
        var redoxConfig = new RedoxConfig();
        var temperature = ((Button)sender).Tag;
        redoxConfig.DataContext = temperature;
        redoxConfig.ShowDialog(App.MainWindow);
    }

    private void OnButtonClickDistance(object sender, RoutedEventArgs e)
    {
        var temperatureConfig = new TemperatureConfig();
        var temperature = ((Button)sender).Tag;
        temperatureConfig.DataContext = temperature;
        temperatureConfig.ShowDialog(App.MainWindow);
    }
}