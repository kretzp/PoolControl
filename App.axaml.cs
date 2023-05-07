using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PoolControl.ViewModels;
using PoolControl.Views;

namespace PoolControl;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            desktop.MainWindow = MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}