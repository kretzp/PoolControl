using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using PoolControl.Helper;
using PoolControl.ViewModels;
using System;

namespace PoolControl.Views;

public partial class MainWindow : Window
{
    private WindowNotificationManager? notificationManager;
    private bool _shutdownStarted;
    private bool _shutdownComplete;
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_shutdownComplete || DataContext is not MainWindowViewModel model) return;
        e.Cancel = true;
        if (_shutdownStarted) return;
        _shutdownStarted = true;
        IsEnabled = false;
        try
        {
            await model.ShutdownAsync();
            _shutdownComplete = true;
            Close();
        }
        catch (Exception ex)
        {
            Log.Logger?.Error(ex, "Application shutdown failed");
            // Do not re-enable hardware commands after a partially completed shutdown.
            notificationManager?.Show(new Notification("Beenden fehlgeschlagen", ex.Message, NotificationType.Error));
        }
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        notificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.BottomRight,
        };
    }

    public void ShowNotification(string title, string message)
    {
        var notif = new Notification(title, message, NotificationType.Success, new (0, 0, 3));
        notificationManager?.Show(notif);
    }
}
