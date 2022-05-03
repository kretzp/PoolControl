using Avalonia;
using Avalonia.Controls;
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
    }
}
