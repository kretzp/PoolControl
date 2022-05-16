using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PoolControl.Pages
{
    public partial class Cistern : UserControl
    {
        public Cistern()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            PoolChart poolChart = new PoolChart();
            poolChart.ShowDialog(App.MainWindow);
        }
    }
}
