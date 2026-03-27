using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bilnex.Pos.Commands;
using Bilnex.Pos.Models;
using Bilnex.Pos.Services;
using Bilnex.Pos.ViewModels.Base;

namespace Bilnex.Pos.ViewModels;

public sealed class ProjectSettingsViewModel : ViewModelBase
{
    private readonly PosSettingsService _settingsService;
    private string _quickAmountsText;
    private string _paymentMethodsText;
    private string _defaultPaymentMethod;
    private string _theme;
    private string _receiptPrinterName;
    private string _statusMessage = "Kaydetmek icin tutarlari virgul ile ayir.";

    public ProjectSettingsViewModel()
    {
        _settingsService = PosSettingsService.Current;
        _quickAmountsText = BuildQuickAmountsText();
        _paymentMethodsText = BuildPaymentMethodsText();
        _defaultPaymentMethod = _settingsService.DefaultPaymentMethod;
        _theme = _settingsService.Theme;
        _receiptPrinterName = _settingsService.ReceiptPrinterName;

        SaveQuickAmountsCommand = new RelayCommand(SaveQuickAmounts);
        ResetQuickAmountsCommand = new RelayCommand(ResetQuickAmounts);
        SavePaymentMethodsCommand = new RelayCommand(SavePaymentMethods);
        ResetPaymentMethodsCommand = new RelayCommand(ResetPaymentMethods);
        SaveGeneralSettingsCommand = new RelayCommand(SaveGeneralSettings);
        ResetGeneralSettingsCommand = new RelayCommand(ResetGeneralSettings);
    }

    public IEnumerable<QuickAmountOption> QuickAmounts => _settingsService.QuickAmounts;

    public IEnumerable<PaymentMethodOption> PaymentMethods => _settingsService.PaymentMethods;

    public string QuickAmountsText
    {
        get => _quickAmountsText;
        set => SetProperty(ref _quickAmountsText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string PaymentMethodsText
    {
        get => _paymentMethodsText;
        set => SetProperty(ref _paymentMethodsText, value);
    }

    public string DefaultPaymentMethod
    {
        get => _defaultPaymentMethod;
        set => SetProperty(ref _defaultPaymentMethod, value);
    }

    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public string ReceiptPrinterName
    {
        get => _receiptPrinterName;
        set => SetProperty(ref _receiptPrinterName, value);
    }

    public ICommand SaveQuickAmountsCommand { get; }

    public ICommand ResetQuickAmountsCommand { get; }

    public ICommand SavePaymentMethodsCommand { get; }

    public ICommand ResetPaymentMethodsCommand { get; }

    public ICommand SaveGeneralSettingsCommand { get; }

    public ICommand ResetGeneralSettingsCommand { get; }

    private void SaveQuickAmounts()
    {
        var parsedAmounts = new List<decimal>();
        var rawItems = QuickAmountsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawItem in rawItems)
        {
            if (!decimal.TryParse(rawItem, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) &&
                !decimal.TryParse(rawItem, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
            {
                StatusMessage = $"Gecersiz tutar: {rawItem}";
                return;
            }

            if (parsedAmount <= 0)
            {
                StatusMessage = $"Tutar sifirdan buyuk olmali: {rawItem}";
                return;
            }

            parsedAmounts.Add(decimal.Truncate(parsedAmount));
        }

        if (parsedAmounts.Count == 0)
        {
            StatusMessage = "En az bir hizli tutar girin.";
            return;
        }

        _settingsService.UpdateQuickAmounts(parsedAmounts);
        OnPropertyChanged(nameof(QuickAmounts));
        QuickAmountsText = BuildQuickAmountsText();
        StatusMessage = "Hizli tutarlar guncellendi.";
    }

    private void ResetQuickAmounts()
    {
        _settingsService.ResetQuickAmounts();
        OnPropertyChanged(nameof(QuickAmounts));
        QuickAmountsText = BuildQuickAmountsText();
        StatusMessage = "Varsayilan hizli tutarlar geri yuklendi.";
    }

    private void SavePaymentMethods()
    {
        var methodKeys = new List<string>();
        var rawItems = PaymentMethodsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawItem in rawItems)
        {
            var normalizedKey = PosSettingsService.NormalizePaymentMethodKey(rawItem);

            if (normalizedKey is null)
            {
                StatusMessage = $"Desteklenmeyen odeme tipi: {rawItem}";
                return;
            }

            methodKeys.Add(normalizedKey);
        }

        if (methodKeys.Count == 0)
        {
            StatusMessage = "En az bir odeme tipi secin.";
            return;
        }

        _settingsService.UpdatePaymentMethods(methodKeys);
        OnPropertyChanged(nameof(PaymentMethods));
        PaymentMethodsText = BuildPaymentMethodsText();
        StatusMessage = "Odeme tipleri guncellendi.";
    }

    private void ResetPaymentMethods()
    {
        _settingsService.ResetPaymentMethods();
        OnPropertyChanged(nameof(PaymentMethods));
        PaymentMethodsText = BuildPaymentMethodsText();
        StatusMessage = "Varsayilan odeme tipleri geri yuklendi.";
        DefaultPaymentMethod = _settingsService.DefaultPaymentMethod;
    }

    private void SaveGeneralSettings()
    {
        var normalizedDefaultPaymentMethod = PosSettingsService.NormalizePaymentMethodKey(DefaultPaymentMethod);

        if (normalizedDefaultPaymentMethod is null)
        {
            StatusMessage = $"Desteklenmeyen varsayilan odeme tipi: {DefaultPaymentMethod}";
            return;
        }

        _settingsService.UpdateGeneralSettings(
            normalizedDefaultPaymentMethod,
            Theme,
            ReceiptPrinterName);

        DefaultPaymentMethod = _settingsService.DefaultPaymentMethod;
        Theme = _settingsService.Theme;
        ReceiptPrinterName = _settingsService.ReceiptPrinterName;
        StatusMessage = "Genel ayarlar guncellendi.";
    }

    private void ResetGeneralSettings()
    {
        _settingsService.ResetGeneralSettings();
        DefaultPaymentMethod = _settingsService.DefaultPaymentMethod;
        Theme = _settingsService.Theme;
        ReceiptPrinterName = _settingsService.ReceiptPrinterName;
        StatusMessage = "Genel ayarlar varsayilana dondu.";
    }

    private string BuildQuickAmountsText()
    {
        return string.Join(", ", _settingsService.QuickAmounts
            .Where(x => x.Value != "TOTAL")
            .Select(x => x.Value));
    }

    private string BuildPaymentMethodsText()
    {
        return string.Join(", ", _settingsService.PaymentMethods.Select(x => x.Key));
    }
}
