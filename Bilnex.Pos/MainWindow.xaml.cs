using System.Windows;
using System.Windows.Input;
using Bilnex.Pos.ViewModels;

namespace Bilnex.Pos;

public partial class MainWindow : Window
{
    private bool _isKioskMode = true;
    private DateTime _lastCtrlClickUtc = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void DragStrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (_isKioskMode)
        {
            return;
        }

        DragMove();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Q)
        {
            return;
        }

        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != (ModifierKeys.Control | ModifierKeys.Shift))
        {
            return;
        }

        ToggleKioskMode();
        e.Handled = true;
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        if ((nowUtc - _lastCtrlClickUtc).TotalMilliseconds <= 450)
        {
            ToggleKioskMode();
            _lastCtrlClickUtc = DateTime.MinValue;
            e.Handled = true;
            return;
        }

        _lastCtrlClickUtc = nowUtc;
    }

    private void ToggleKioskMode()
    {
        if (_isKioskMode)
        {
            Topmost = false;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
            Width = Math.Max(1280, ActualWidth);
            Height = Math.Max(820, ActualHeight);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _isKioskMode = false;
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        WindowState = WindowState.Maximized;
        _isKioskMode = true;
    }
}
