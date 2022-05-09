using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace PoolControl.Pages
{
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
            TemperatureConfig temperatureConfig = new TemperatureConfig();
            object temperature = ((Button)sender).Tag;
            temperatureConfig.DataContext = temperature;
            temperatureConfig.ShowDialog(App.MainWindow);
        }

        private void OnButtonClickPh(object sender, RoutedEventArgs e)
        {
            PhConfig pHConfig = new PhConfig();
            object temperature = ((Button)sender).Tag;
            pHConfig.DataContext = temperature;
            pHConfig.ShowDialog(App.MainWindow);
        }

        private void OnButtonClickRedox(object sender, RoutedEventArgs e)
        {
            RedoxConfig redoxConfig = new RedoxConfig();
            object temperature = ((Button)sender).Tag;
            redoxConfig.DataContext = temperature;
            redoxConfig.ShowDialog(App.MainWindow);
        }

        private void OnButtonClickDistance(object sender, RoutedEventArgs e)
        {
            TemperatureConfig temperatureConfig = new TemperatureConfig();
            object temperature = ((Button)sender).Tag;
            temperatureConfig.DataContext = temperature;
            temperatureConfig.ShowDialog(App.MainWindow);
        }
    }
}
