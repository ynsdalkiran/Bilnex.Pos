using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bilnex.Pos.Services;

namespace Bilnex.Pos.Views;

public partial class TouchDialogWindow : Window
{
    private DispatcherTimer? _autoCloseTimer;
    private int _secondsRemaining;

    public TouchDialogWindow()
    {
        InitializeComponent();
        Loaded += TouchDialogWindow_Loaded;
        Closed += TouchDialogWindow_Closed;
    }

    public string TitleText { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public AppDialogKind DialogKind { get; set; } = AppDialogKind.Info;

    public AppDialogButtons DialogButtons { get; set; } = AppDialogButtons.Ok;

    public string PrimaryButtonText { get; set; } = "Tamam";

    public string SecondaryButtonText { get; set; } = "Vazge\u00E7";

    public string? ErrorCode { get; set; }

    public int? AutoCloseSeconds { get; set; }

    public bool PlaySound { get; set; }

    public bool KioskMode { get; set; }

    public AppDialogResult Result { get; private set; } = AppDialogResult.None;

    private void TouchDialogWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBlock.Text = TitleText;
        MessageBlock.Text = MessageText;
        PrimaryButtonTextBlock.Text = PrimaryButtonText;
        SecondaryButtonLabel.Text = SecondaryButtonText;

        ConfigureButtons();
        ConfigureKind();
        ConfigureErrorCode();
        ConfigureKioskMode();
        ConfigureAutoClose();
        PlayDialogSound();
        Dispatcher.BeginInvoke(() => Keyboard.Focus(PrimaryButton), DispatcherPriority.Input);
    }

    private void ConfigureButtons()
    {
        SecondaryButton.Visibility = DialogButtons == AppDialogButtons.Ok
            ? Visibility.Collapsed
            : Visibility.Visible;

        PrimaryButton.Style = (Style)FindResource("PrimaryActionButtonStyle");
        SecondaryButton.Style = (Style)FindResource("SecondaryActionButtonStyle");
        PrimaryButtonIcon.Text = "\uE8FB";
        SecondaryButtonIcon.Text = "\uE711";

        if (DialogButtons == AppDialogButtons.Ok)
        {
            Grid.SetColumn(PrimaryButton, 0);
            Grid.SetColumnSpan(PrimaryButton, 3);
        }
    }

    private void ConfigureKind()
    {
        switch (DialogKind)
        {
            case AppDialogKind.Success:
                AccentBadge.Background = (Brush)FindResource("MetroTileGreenBrush");
                TopAccentBar.Background = (Brush)FindResource("MetroTileGreenBrush");
                StatusBadge.Background = (Brush)FindResource("MetroTileGreenBrush");
                StatusBadgeText.Text = "BA\u015EARILI";
                PrimaryButton.Style = (Style)FindResource("SuccessActionButtonStyle");
                PrimaryButtonIcon.Text = "\uE73E";
                IconText.Text = "\uE73E";
                CaptionBlock.Text = "\u0130\u015Flem tamamland\u0131";
                break;
            case AppDialogKind.Warning:
                AccentBadge.Background = (Brush)FindResource("MetroTileOrangeBrush");
                TopAccentBar.Background = (Brush)FindResource("MetroTileOrangeBrush");
                StatusBadge.Background = (Brush)FindResource("MetroTileOrangeBrush");
                StatusBadgeText.Text = "UYARI";
                PrimaryButton.Style = (Style)FindResource("WarningActionButtonStyle");
                PrimaryButtonIcon.Text = "\uE7BA";
                IconText.Text = "\uE7BA";
                CaptionBlock.Text = "Dikkat gerektiren i\u015Flem";
                break;
            case AppDialogKind.Danger:
                AccentBadge.Background = (Brush)FindResource("ShellExitBackgroundBrush");
                TopAccentBar.Background = (Brush)FindResource("ShellExitBackgroundBrush");
                StatusBadge.Background = (Brush)FindResource("ShellExitBackgroundBrush");
                StatusBadgeText.Text = "HATA";
                PrimaryButton.Style = (Style)FindResource("DangerActionButtonStyle");
                PrimaryButtonIcon.Text = "\uEA39";
                IconText.Text = "\uEA39";
                CaptionBlock.Text = "Uyar\u0131";
                break;
            default:
                AccentBadge.Background = (Brush)FindResource("MetroTileBlueBrush");
                TopAccentBar.Background = (Brush)FindResource("MetroTileBlueBrush");
                StatusBadge.Background = (Brush)FindResource("MetroTileBlueBrush");
                StatusBadgeText.Text = "B\u0130LG\u0130";
                PrimaryButton.Style = (Style)FindResource("PrimaryActionButtonStyle");
                PrimaryButtonIcon.Text = "\uE8FB";
                IconText.Text = "\uE946";
                CaptionBlock.Text = "Bilnex sistem bildirimi";
                break;
        }
    }

    private void ConfigureErrorCode()
    {
        if (string.IsNullOrWhiteSpace(ErrorCode))
        {
            ErrorCodeBlock.Visibility = Visibility.Collapsed;
            return;
        }

        ErrorCodeBlock.Text = $"Kod: {ErrorCode}";
        ErrorCodeBlock.Visibility = Visibility.Visible;
    }

    private void ConfigureKioskMode()
    {
        if (!KioskMode)
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowState = WindowState.Maximized;
        DialogContainer.Width = double.NaN;
        DialogContainer.MaxWidth = 920;
        DialogContainer.Margin = new Thickness(48);
        MessageBlock.FontSize = 28;
        PrimaryButton.Height = 96;
        SecondaryButton.Height = 96;
    }

    private void ConfigureAutoClose()
    {
        if (!AutoCloseSeconds.HasValue || AutoCloseSeconds.Value <= 0)
        {
            CountdownBlock.Visibility = Visibility.Collapsed;
            return;
        }

        _secondsRemaining = AutoCloseSeconds.Value;
        CountdownBlock.Visibility = Visibility.Visible;
        UpdateCountdownText();

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        _autoCloseTimer.Start();
    }

    private void PlayDialogSound()
    {
        if (!PlaySound)
        {
            return;
        }

        switch (DialogKind)
        {
            case AppDialogKind.Success:
                SystemSounds.Asterisk.Play();
                break;
            case AppDialogKind.Warning:
                SystemSounds.Exclamation.Play();
                break;
            case AppDialogKind.Danger:
                SystemSounds.Hand.Play();
                break;
            default:
                SystemSounds.Beep.Play();
                break;
        }
    }

    private void AutoCloseTimer_Tick(object? sender, EventArgs e)
    {
        _secondsRemaining--;

        if (_secondsRemaining <= 0)
        {
            Result = AppDialogResult.Ok;
            DialogResult = true;
            Close();
            return;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        CountdownBlock.Text = $"Otomatik kapan\u0131\u015F: {_secondsRemaining} sn";
    }

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !KioskMode)
        {
            DragMove();
        }
    }

    private void TouchDialogWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PrimaryButton_Click(PrimaryButton, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (DialogButtons == AppDialogButtons.OkCancel)
            {
                SecondaryButton_Click(SecondaryButton, new RoutedEventArgs());
            }
            else
            {
                PrimaryButton_Click(PrimaryButton, new RoutedEventArgs());
            }

            e.Handled = true;
        }
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = AppDialogResult.Ok;
        DialogResult = true;
        Close();
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = AppDialogResult.Cancel;
        DialogResult = false;
        Close();
    }

    private void TouchDialogWindow_Closed(object? sender, EventArgs e)
    {
        if (_autoCloseTimer is null)
        {
            return;
        }

        _autoCloseTimer.Stop();
        _autoCloseTimer.Tick -= AutoCloseTimer_Tick;
        _autoCloseTimer = null;
    }
}
