using System.Linq;
using System.Windows;
using Bilnex.Pos.Views;

namespace Bilnex.Pos.Services;

public static class AppDialogService
{
    public static void ShowToastInfo(string title, string message, int autoCloseSeconds = 4)
    {
        AppNotificationService.Current.ShowToast(title, message, AppDialogKind.Info, autoCloseSeconds);
    }

    public static void ShowToastSuccess(string title, string message, int autoCloseSeconds = 4)
    {
        AppNotificationService.Current.ShowToast(title, message, AppDialogKind.Success, autoCloseSeconds);
    }

    public static void ShowToastWarning(string title, string message, int autoCloseSeconds = 5)
    {
        AppNotificationService.Current.ShowToast(title, message, AppDialogKind.Warning, autoCloseSeconds);
    }

    public static void ShowToastError(string title, string message, int autoCloseSeconds = 5)
    {
        AppNotificationService.Current.ShowToast(title, message, AppDialogKind.Danger, autoCloseSeconds);
    }

    public static void ShowFullscreenAlert(string title, string message, AppDialogKind kind, string buttonText = "Tamam")
    {
        AppNotificationService.Current.ShowOverlay(title, message, kind, buttonText);
    }

    public static bool ShowDeleteConfirmation(string itemName, string? detail = null)
    {
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"'{itemName}' kayd\u0131 silinecek. Bu i\u015Flem geri al\u0131namaz."
            : $"'{itemName}' kayd\u0131 silinecek.\n{detail}";

        var result = Show(
            "Silme Onay\u0131",
            message,
            AppDialogKind.Danger,
            AppDialogButtons.OkCancel,
            new AppDialogOptions
            {
                PrimaryButtonText = "Sil",
                SecondaryButtonText = "Vazge\u00E7",
                PlaySound = true,
                ErrorCode = "DLG-DELETE"
            });

        return result == AppDialogResult.Ok;
    }

    public static bool ShowResetConfirmation(string scopeName)
    {
        var result = Show(
            "Varsay\u0131lana D\u00F6n",
            $"{scopeName} ayarlar\u0131 varsay\u0131lan de\u011Ferlere d\u00F6nd\u00FCr\u00FClecek. Devam etmek istiyor musunuz?",
            AppDialogKind.Warning,
            AppDialogButtons.OkCancel,
            new AppDialogOptions
            {
                PrimaryButtonText = "S\u0131f\u0131rla",
                SecondaryButtonText = "Vazge\u00E7",
                PlaySound = true,
                ErrorCode = "DLG-RESET"
            });

        return result == AppDialogResult.Ok;
    }

    public static AppDialogResult ShowOperationFailed(string title, string message, string? errorCode = null)
    {
        return Show(
            title,
            message,
            AppDialogKind.Danger,
            AppDialogButtons.Ok,
            new AppDialogOptions
            {
                PrimaryButtonText = "Tamam",
                PlaySound = true,
                ErrorCode = errorCode ?? "ERR-GENERAL"
            });
    }

    public static AppDialogResult ShowOfflineWarning(bool kioskMode = false)
    {
        if (kioskMode)
        {
            ShowFullscreenAlert(
                "Ba\u011Flant\u0131 Uyar\u0131s\u0131",
                "A\u011F ba\u011Flant\u0131s\u0131 bulunamad\u0131. \u0130\u015Flem yerel modda devam edecek.",
                AppDialogKind.Warning);

            return AppDialogResult.Ok;
        }

        return Show(
            "Ba\u011Flant\u0131 Uyar\u0131s\u0131",
            "A\u011F ba\u011Flant\u0131s\u0131 bulunamad\u0131. \u0130\u015Flem yerel modda devam edecek.",
            AppDialogKind.Warning,
            AppDialogButtons.Ok,
            new AppDialogOptions
            {
                PrimaryButtonText = "Tamam",
                PlaySound = true,
                AutoCloseSeconds = 8,
                ErrorCode = "NET-OFFLINE",
                KioskMode = kioskMode
            });
    }

    public static AppDialogResult ShowSaved(string scopeName)
    {
        ShowToastSuccess("Kay\u0131t Tamamland\u0131", $"{scopeName} ba\u015Far\u0131yla kaydedildi.", 3);
        return AppDialogResult.Ok;
    }

    public static AppDialogResult ShowInfo(string title, string message)
    {
        return Show(title, message, AppDialogKind.Info, AppDialogButtons.Ok);
    }

    public static AppDialogResult ShowSuccess(string title, string message)
    {
        return Show(title, message, AppDialogKind.Success, AppDialogButtons.Ok);
    }

    public static AppDialogResult ShowWarning(string title, string message)
    {
        return Show(title, message, AppDialogKind.Warning, AppDialogButtons.Ok, new AppDialogOptions { PlaySound = true });
    }

    public static AppDialogResult ShowError(string title, string message)
    {
        return Show(title, message, AppDialogKind.Danger, AppDialogButtons.Ok, new AppDialogOptions { PlaySound = true });
    }

    public static bool ShowConfirmation(string title, string message, string confirmText = "Onayla", string cancelText = "Vazge\u00E7")
    {
        var result = Show(
            title,
            message,
            AppDialogKind.Warning,
            AppDialogButtons.OkCancel,
            new AppDialogOptions
            {
                PrimaryButtonText = confirmText,
                SecondaryButtonText = cancelText,
                PlaySound = true
            });

        return result == AppDialogResult.Ok;
    }

    public static AppDialogResult Show(
        string title,
        string message,
        AppDialogKind kind,
        AppDialogButtons buttons,
        AppDialogOptions? options = null)
    {
        options ??= new AppDialogOptions();

        var dialog = new TouchDialogWindow
        {
            Owner = ResolveOwner(),
            TitleText = title,
            MessageText = message,
            DialogKind = kind,
            DialogButtons = buttons,
            PrimaryButtonText = options.PrimaryButtonText,
            SecondaryButtonText = options.SecondaryButtonText,
            ErrorCode = options.ErrorCode,
            AutoCloseSeconds = options.AutoCloseSeconds,
            PlaySound = options.PlaySound,
            KioskMode = options.KioskMode
        };

        dialog.ShowDialog();
        return dialog.Result;
    }

    private static Window? ResolveOwner()
    {
        return Application.Current?
            .Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
            ?? Application.Current?.MainWindow;
    }
}

public sealed class AppDialogOptions
{
    public string PrimaryButtonText { get; init; } = "Tamam";

    public string SecondaryButtonText { get; init; } = "Vazge\u00E7";

    public string? ErrorCode { get; init; }

    public int? AutoCloseSeconds { get; init; }

    public bool PlaySound { get; init; }

    public bool KioskMode { get; init; }
}

public enum AppDialogKind
{
    Info,
    Success,
    Warning,
    Danger
}

public enum AppDialogButtons
{
    Ok,
    OkCancel
}

public enum AppDialogResult
{
    None,
    Ok,
    Cancel
}
