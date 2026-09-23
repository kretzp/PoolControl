using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using PoolControl.Helper;

namespace PoolControl.Views;

/// <summary>Switches views only. The inherited MainWindowViewModel stays alive.</summary>
public sealed class SurfaceHost : UserControl
{
    private readonly UiPreferences _preferences;
    private readonly string? _preferencesPath;
    private ModernView? _modern;
    private MainView? _classic;

    public SurfaceHost() : this(UiPreferences.Load(), null) { }

    public SurfaceHost(UiPreferences preferences, string? preferencesPath)
    {
        _preferences = preferences;
        _preferencesPath = preferencesPath;
        ShowSurface();
    }

    public void SelectSurface(string surface)
    {
        _preferences.Surface = surface;
        SavePreferences();
        ShowSurface();
    }

    private void ShowSurface()
    {
        if (_classic?.Parent is Panel oldPanel) oldPanel.Children.Clear();
        if (_preferences.Surface == "Classic")
        {
            _classic ??= new MainView();
            _classic.VerticalAlignment = VerticalAlignment.Center;
            var panel = new Panel();
            panel.Children.Add(_classic);
            var change = new Button
            {
                Content = "Modern", Name = "SwitchToModern", MinHeight = 52, Width = 116,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(6, 0, 0, 20), FontSize = 17
            };
            change.Click += (_, _) => SelectSurface("Modern");
            panel.Children.Add(change);
            Content = new ThemeVariantScope { RequestedThemeVariant = ThemeVariant.Dark, Child = panel };
        }
        else
        {
            // Detach the classic view from its temporary wrapper before the next switch.
            if (Content is Panel panel) panel.Children.Clear();
            _modern ??= new ModernView(_preferences, SavePreferences, () => SelectSurface("Classic"));
            Content = _modern;
        }
    }

    private void SavePreferences()
    {
        try { _preferences.Save(_preferencesPath); }
        catch (Exception ex)
        {
            Log.Logger?.Warning(ex, "Could not save display preferences");
            (App.MainWindow as MainWindow)?.ShowNotification("PoolControl", _preferences.Language == "de"
                ? "Anzeigeeinstellungen konnten nicht gespeichert werden."
                : "Display preferences could not be saved.");
        }
    }
}
