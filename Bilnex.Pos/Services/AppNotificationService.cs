using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bilnex.Pos.Commands;
using Bilnex.Pos.ViewModels.Base;

namespace Bilnex.Pos.Services;

public sealed class AppNotificationService : ViewModelBase
{
    private OverlayNotificationItem? _activeOverlay;

    private AppNotificationService()
    {
        DismissOverlayCommand = new RelayCommand(DismissOverlay);
    }

    public static AppNotificationService Current { get; } = new();

    public ObservableCollection<ToastNotificationItem> Toasts { get; } = [];

    public ICommand DismissOverlayCommand { get; }

    public OverlayNotificationItem? ActiveOverlay
    {
        get => _activeOverlay;
        private set
        {
            if (SetProperty(ref _activeOverlay, value))
            {
                OnPropertyChanged(nameof(HasActiveOverlay));
            }
        }
    }

    public bool HasActiveOverlay => ActiveOverlay is not null;

    public void ShowToast(string title, string message, AppDialogKind kind, int autoCloseSeconds = 4)
    {
        RunOnUiThread(() =>
        {
            var visual = CreateVisual(kind);
            var item = new ToastNotificationItem
            {
                Title = title,
                Message = message,
                Caption = visual.Caption,
                IconGlyph = visual.IconGlyph,
                AccentBrush = visual.AccentBrush,
                AccentLabel = visual.AccentLabel
            };

            Toasts.Add(item);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(2, autoCloseSeconds))
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Toasts.Remove(item);
            };

            timer.Start();
        });
    }

    public void ShowOverlay(string title, string message, AppDialogKind kind, string buttonText = "Tamam")
    {
        RunOnUiThread(() =>
        {
            var visual = CreateVisual(kind);
            ActiveOverlay = new OverlayNotificationItem
            {
                Title = title,
                Message = message,
                Caption = visual.Caption,
                IconGlyph = visual.IconGlyph,
                AccentBrush = visual.AccentBrush,
                AccentLabel = visual.AccentLabel,
                ButtonText = buttonText
            };
        });
    }

    public void DismissOverlay()
    {
        RunOnUiThread(() => ActiveOverlay = null);
    }

    private static NotificationVisual CreateVisual(AppDialogKind kind)
    {
        var application = Application.Current;

        return kind switch
        {
            AppDialogKind.Success => new NotificationVisual(
                (Brush)application.FindResource("MetroTileGreenBrush"),
                "\uE73E",
                "BA\u015EARILI",
                "\u0130\u015Flem tamamland\u0131"),
            AppDialogKind.Warning => new NotificationVisual(
                (Brush)application.FindResource("MetroTileOrangeBrush"),
                "\uE7BA",
                "UYARI",
                "Dikkat gerektiren i\u015Flem"),
            AppDialogKind.Danger => new NotificationVisual(
                (Brush)application.FindResource("ShellExitBackgroundBrush"),
                "\uEA39",
                "HATA",
                "Kritik i\u015Flem uyar\u0131s\u0131"),
            _ => new NotificationVisual(
                (Brush)application.FindResource("MetroTileBlueBrush"),
                "\uE946",
                "B\u0130LG\u0130",
                "Bilnex sistem bildirimi")
        };
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private readonly record struct NotificationVisual(
        Brush AccentBrush,
        string IconGlyph,
        string AccentLabel,
        string Caption);
}

public sealed class ToastNotificationItem : ViewModelBase
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private string _caption = string.Empty;
    private string _iconGlyph = string.Empty;
    private string _accentLabel = string.Empty;
    private Brush _accentBrush = Brushes.DodgerBlue;

    public string Title
    {
        get => _title;
        init => _title = value;
    }

    public string Message
    {
        get => _message;
        init => _message = value;
    }

    public string Caption
    {
        get => _caption;
        init => _caption = value;
    }

    public string IconGlyph
    {
        get => _iconGlyph;
        init => _iconGlyph = value;
    }

    public string AccentLabel
    {
        get => _accentLabel;
        init => _accentLabel = value;
    }

    public Brush AccentBrush
    {
        get => _accentBrush;
        init => _accentBrush = value;
    }
}

public sealed class OverlayNotificationItem : ViewModelBase
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private string _caption = string.Empty;
    private string _iconGlyph = string.Empty;
    private string _accentLabel = string.Empty;
    private string _buttonText = "Tamam";
    private Brush _accentBrush = Brushes.DodgerBlue;

    public string Title
    {
        get => _title;
        init => _title = value;
    }

    public string Message
    {
        get => _message;
        init => _message = value;
    }

    public string Caption
    {
        get => _caption;
        init => _caption = value;
    }

    public string IconGlyph
    {
        get => _iconGlyph;
        init => _iconGlyph = value;
    }

    public string AccentLabel
    {
        get => _accentLabel;
        init => _accentLabel = value;
    }

    public string ButtonText
    {
        get => _buttonText;
        init => _buttonText = value;
    }

    public Brush AccentBrush
    {
        get => _accentBrush;
        init => _accentBrush = value;
    }
}
