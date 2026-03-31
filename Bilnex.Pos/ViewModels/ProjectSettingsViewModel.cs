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
    private static readonly IReadOnlyList<string> _themeOptions = new[] { "Dark", "Light" };
    private static readonly IReadOnlyList<string> _priceChangeModeOptions = new[] { "Percentage", "Fixed" };
    private static readonly IReadOnlyList<string> _labelTemplateOptions = new[] { "50x30", "58x40", "100x70" };
    private readonly PosSettingsService _settingsService;
    private string _quickAmountsText;
    private string _paymentMethodsText;
    private string _defaultPaymentMethod;
    private string _theme;
    private string _receiptPrinterName;
    private string _priceChangeMode;
    private string _priceChangeValue;
    private bool _requirePriceApproval;
    private string _labelTemplate;
    private string _labelPrinterName;
    private string _labelCopyCount;
    private bool _printBarcodeOnLabel;
    private string _statusMessage = "Kaydetmek icin tutarlari virgul ile ayir.";

    public ProjectSettingsViewModel()
    {
        _settingsService = PosSettingsService.Current;
        _quickAmountsText = BuildQuickAmountsText();
        _paymentMethodsText = BuildPaymentMethodsText();
        _defaultPaymentMethod = _settingsService.DefaultPaymentMethod;
        _theme = _settingsService.Theme;
        _receiptPrinterName = _settingsService.ReceiptPrinterName;
        _priceChangeMode = _settingsService.PriceChangeMode;
        _priceChangeValue = _settingsService.PriceChangeValue.ToString("0.##", CultureInfo.InvariantCulture);
        _requirePriceApproval = _settingsService.RequirePriceApproval;
        _labelTemplate = _settingsService.LabelTemplate;
        _labelPrinterName = _settingsService.LabelPrinterName;
        _labelCopyCount = _settingsService.LabelCopyCount.ToString(CultureInfo.InvariantCulture);
        _printBarcodeOnLabel = _settingsService.PrintBarcodeOnLabel;

        SaveQuickAmountsCommand = new RelayCommand(SaveQuickAmounts);
        ResetQuickAmountsCommand = new RelayCommand(ResetQuickAmounts);
        SavePaymentMethodsCommand = new RelayCommand(SavePaymentMethods);
        ResetPaymentMethodsCommand = new RelayCommand(ResetPaymentMethods);
        SaveGeneralSettingsCommand = new RelayCommand(SaveGeneralSettings);
        ResetGeneralSettingsCommand = new RelayCommand(ResetGeneralSettings);
        SavePriceChangeSettingsCommand = new RelayCommand(SavePriceChangeSettings);
        ResetPriceChangeSettingsCommand = new RelayCommand(ResetPriceChangeSettings);
        SaveLabelPrintSettingsCommand = new RelayCommand(SaveLabelPrintSettings);
        ResetLabelPrintSettingsCommand = new RelayCommand(ResetLabelPrintSettings);
    }

    public IEnumerable<QuickAmountOption> QuickAmounts => _settingsService.QuickAmounts;

    public IEnumerable<PaymentMethodOption> PaymentMethods => _settingsService.PaymentMethods;

    public IEnumerable<string> DefaultPaymentMethodOptions => _settingsService.PaymentMethods.Select(x => x.Key);

    public IEnumerable<string> ThemeOptions => _themeOptions;

    public IEnumerable<string> PriceChangeModeOptions => _priceChangeModeOptions;

    public IEnumerable<string> LabelTemplateOptions => _labelTemplateOptions;

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

    public string PriceChangeMode
    {
        get => _priceChangeMode;
        set => SetProperty(ref _priceChangeMode, value);
    }

    public string PriceChangeValue
    {
        get => _priceChangeValue;
        set => SetProperty(ref _priceChangeValue, value);
    }

    public bool RequirePriceApproval
    {
        get => _requirePriceApproval;
        set => SetProperty(ref _requirePriceApproval, value);
    }

    public string LabelTemplate
    {
        get => _labelTemplate;
        set => SetProperty(ref _labelTemplate, value);
    }

    public string LabelPrinterName
    {
        get => _labelPrinterName;
        set => SetProperty(ref _labelPrinterName, value);
    }

    public string LabelCopyCount
    {
        get => _labelCopyCount;
        set => SetProperty(ref _labelCopyCount, value);
    }

    public bool PrintBarcodeOnLabel
    {
        get => _printBarcodeOnLabel;
        set => SetProperty(ref _printBarcodeOnLabel, value);
    }

    public ICommand SaveQuickAmountsCommand { get; }

    public ICommand ResetQuickAmountsCommand { get; }

    public ICommand SavePaymentMethodsCommand { get; }

    public ICommand ResetPaymentMethodsCommand { get; }

    public ICommand SaveGeneralSettingsCommand { get; }

    public ICommand ResetGeneralSettingsCommand { get; }

    public ICommand SavePriceChangeSettingsCommand { get; }

    public ICommand ResetPriceChangeSettingsCommand { get; }

    public ICommand SaveLabelPrintSettingsCommand { get; }

    public ICommand ResetLabelPrintSettingsCommand { get; }

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
        OnPropertyChanged(nameof(DefaultPaymentMethodOptions));
        PaymentMethodsText = BuildPaymentMethodsText();
        StatusMessage = "Odeme tipleri guncellendi.";
    }

    private void ResetPaymentMethods()
    {
        _settingsService.ResetPaymentMethods();
        OnPropertyChanged(nameof(PaymentMethods));
        OnPropertyChanged(nameof(DefaultPaymentMethodOptions));
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

        if (!ThemeOptions.Contains(Theme))
        {
            StatusMessage = $"Desteklenmeyen tema: {Theme}";
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

    private void SavePriceChangeSettings()
    {
        if (!PriceChangeModeOptions.Contains(PriceChangeMode))
        {
            StatusMessage = $"Desteklenmeyen fiyat degisim tipi: {PriceChangeMode}";
            return;
        }

        if (!decimal.TryParse(PriceChangeValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue) &&
            !decimal.TryParse(PriceChangeValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedValue))
        {
            StatusMessage = $"Gecersiz fiyat degisim degeri: {PriceChangeValue}";
            return;
        }

        _settingsService.UpdatePriceChangeSettings(PriceChangeMode, parsedValue, RequirePriceApproval);
        PriceChangeMode = _settingsService.PriceChangeMode;
        PriceChangeValue = _settingsService.PriceChangeValue.ToString("0.##", CultureInfo.InvariantCulture);
        RequirePriceApproval = _settingsService.RequirePriceApproval;
        StatusMessage = "Fiyat degisimi ayarlari guncellendi.";
    }

    private void ResetPriceChangeSettings()
    {
        _settingsService.ResetPriceChangeSettings();
        PriceChangeMode = _settingsService.PriceChangeMode;
        PriceChangeValue = _settingsService.PriceChangeValue.ToString("0.##", CultureInfo.InvariantCulture);
        RequirePriceApproval = _settingsService.RequirePriceApproval;
        StatusMessage = "Fiyat degisimi ayarlari varsayilana dondu.";
    }

    private void SaveLabelPrintSettings()
    {
        if (!LabelTemplateOptions.Contains(LabelTemplate))
        {
            StatusMessage = $"Desteklenmeyen etiket sablonu: {LabelTemplate}";
            return;
        }

        if (!int.TryParse(LabelCopyCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCopyCount) &&
            !int.TryParse(LabelCopyCount, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsedCopyCount))
        {
            StatusMessage = $"Gecersiz etiket kopya sayisi: {LabelCopyCount}";
            return;
        }

        _settingsService.UpdateLabelPrintSettings(LabelTemplate, LabelPrinterName, parsedCopyCount, PrintBarcodeOnLabel);
        LabelTemplate = _settingsService.LabelTemplate;
        LabelPrinterName = _settingsService.LabelPrinterName;
        LabelCopyCount = _settingsService.LabelCopyCount.ToString(CultureInfo.InvariantCulture);
        PrintBarcodeOnLabel = _settingsService.PrintBarcodeOnLabel;
        StatusMessage = "Etiket basimi ayarlari guncellendi.";
    }

    private void ResetLabelPrintSettings()
    {
        _settingsService.ResetLabelPrintSettings();
        LabelTemplate = _settingsService.LabelTemplate;
        LabelPrinterName = _settingsService.LabelPrinterName;
        LabelCopyCount = _settingsService.LabelCopyCount.ToString(CultureInfo.InvariantCulture);
        PrintBarcodeOnLabel = _settingsService.PrintBarcodeOnLabel;
        StatusMessage = "Etiket basimi ayarlari varsayilana dondu.";
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
