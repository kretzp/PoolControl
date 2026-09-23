using Avalonia;
using Avalonia.Controls;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PoolControl.Communication;
using PoolControl.ViewModels;
using PoolControl.Views;

namespace PoolControl;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            desktop.MainWindow = MainWindow;
            viewModel.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Deserialized child view models use PoolMqttClient.Instance as their fallback.
        // Register that same object so DI-managed view models share one connection.
        services.AddSingleton<IPoolMqttClient>(PoolMqttClient.Instance);
        services.AddSingleton<MainWindowViewModel>();
    }
}
