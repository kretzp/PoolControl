using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using PoolControl.Helper;

namespace PoolControl.Views;

public partial class MainWindow : Window
{
    private readonly WindowNotificationManager? notificationManager;
    public MainWindow()
    {
        InitializeComponent();
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