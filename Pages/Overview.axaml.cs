using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
    }
}
