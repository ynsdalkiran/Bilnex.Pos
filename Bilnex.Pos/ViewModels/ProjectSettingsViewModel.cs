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
    private static readonly IReadOnlyList<SelectionOption> _themeOptions =
    [
        new("Dark", "Koyu"),
        new("Light", "Açık")
    ];
    private static readonly IReadOnlyList<SelectionOption> _priceChangeModeOptions =
    [
        new("Percentage", "Yüzde"),
        new("Fixed", "Tutar")
    ];
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
    private string _quickProductColumns;
    private string _quickProductMinButtonHeight;
    private string _quickProductButtonFontSize;
    private string _quickProductCategoryColumns;
    private string _discountDisplayMode;
    private string _basketPosition;
    private string _statusMessage = "Kaydetmek için tutarları virgül ile ayır.";

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
        _quickProductColumns = _settingsService.QuickProductColumns.ToString(CultureInfo.InvariantCulture);
        _quickProductMinButtonHeight = _settingsService.QuickProductMinButtonHeight.ToString(CultureInfo.InvariantCulture);
        _quickProductButtonFontSize = _settingsService.QuickProductButtonFontSize.ToString(CultureInfo.InvariantCulture);
        _quickProductCategoryColumns = _settingsService.QuickProductCategoryColumns.ToString(CultureInfo.InvariantCulture);
        _discountDisplayMode = _settingsService.DiscountDisplayMode;
        _basketPosition = _settingsService.BasketPosition;

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
        SaveQuickProductLayoutCommand = new RelayCommand(SaveQuickProductLayout);
        ResetQuickProductLayoutCommand = new RelayCommand(ResetQuickProductLayout);
        SavePosLayoutCommand = new RelayCommand(SavePosLayoutSettings);
        ResetPosLayoutCommand = new RelayCommand(ResetPosLayoutSettings);
    }

    public IEnumerable<QuickAmountOption> QuickAmounts => _settingsService.QuickAmounts;

    public IEnumerable<PaymentMethodOption> PaymentMethods => _settingsService.PaymentMethods;

    public IEnumerable<PaymentMethodOption> DefaultPaymentMethodOptions => _settingsService.PaymentMethods;

    public IEnumerable<SelectionOption> ThemeOptions => _themeOptions;

    public IEnumerable<SelectionOption> PriceChangeModeOptions => _priceChangeModeOptions;

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
        set
        {
            if (SetProperty(ref _defaultPaymentMethod, value))
            {
                OnPropertyChanged(nameof(DefaultPaymentMethodDisplay));
            }
        }
    }

    public string Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                OnPropertyChanged(nameof(ThemeDisplay));
            }
        }
    }

    public string ReceiptPrinterName
    {
        get => _receiptPrinterName;
        set => SetProperty(ref _receiptPrinterName, value);
    }

    public string PriceChangeMode
    {
        get => _priceChangeMode;
        set
        {
            if (SetProperty(ref _priceChangeMode, value))
            {
                OnPropertyChanged(nameof(PriceChangeModeDisplay));
            }
        }
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

    public string QuickProductColumns
    {
        get => _quickProductColumns;
        set => SetProperty(ref _quickProductColumns, value);
    }

    public string QuickProductMinButtonHeight
    {
        get => _quickProductMinButtonHeight;
        set => SetProperty(ref _quickProductMinButtonHeight, value);
    }

    public string QuickProductButtonFontSize
    {
        get => _quickProductButtonFontSize;
        set => SetProperty(ref _quickProductButtonFontSize, value);
    }

    public string QuickProductCategoryColumns
    {
        get => _quickProductCategoryColumns;
        set => SetProperty(ref _quickProductCategoryColumns, value);
    }

    public string DefaultPaymentMethodDisplay => ResolvePaymentMethodTitle(DefaultPaymentMethod);

    public string ThemeDisplay => ResolveThemeTitle(Theme);

    public string PriceChangeModeDisplay => ResolvePriceChangeModeTitle(PriceChangeMode);

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

    public ICommand SaveQuickProductLayoutCommand { get; }

    public ICommand ResetQuickProductLayoutCommand { get; }

    public ICommand SavePosLayoutCommand { get; }

    public ICommand ResetPosLayoutCommand { get; }

    public bool IsDiscountDisplayStrip
    {
        get => _discountDisplayMode == "Strip";
        set
        {
            if (value)
            {
                _discountDisplayMode = "Strip";
                OnPropertyChanged(nameof(IsDiscountDisplayStrip));
                OnPropertyChanged(nameof(IsDiscountDisplayTotalCard));
            }
        }
    }

    public bool IsDiscountDisplayTotalCard
    {
        get => _discountDisplayMode == "TotalCard";
        set
        {
            if (value)
            {
                _discountDisplayMode = "TotalCard";
                OnPropertyChanged(nameof(IsDiscountDisplayStrip));
                OnPropertyChanged(nameof(IsDiscountDisplayTotalCard));
            }
        }
    }

    public bool IsBasketLeft
    {
        get => _basketPosition == "Left";
        set
        {
            if (value)
            {
                _basketPosition = "Left";
                OnPropertyChanged(nameof(IsBasketLeft));
                OnPropertyChanged(nameof(IsBasketRight));
            }
        }
    }

    public bool IsBasketRight
    {
        get => _basketPosition == "Right";
        set
        {
            if (value)
            {
                _basketPosition = "Right";
                OnPropertyChanged(nameof(IsBasketLeft));
                OnPropertyChanged(nameof(IsBasketRight));
            }
        }
    }

    private void SavePosLayoutSettings()
    {
        _settingsService.UpdatePosLayoutSettings(_discountDisplayMode, _basketPosition);
        StatusMessage = "POS görünüm ayarları kaydedildi.";
        AppDialogService.ShowSaved("POS görünüm ayarları");
    }

    private void ResetPosLayoutSettings()
    {
        if (!AppDialogService.ShowResetConfirmation("POS görünüm ayarları"))
        {
            return;
        }

        _settingsService.ResetPosLayoutSettings();
        _discountDisplayMode = _settingsService.DiscountDisplayMode;
        _basketPosition = _settingsService.BasketPosition;
        OnPropertyChanged(nameof(IsDiscountDisplayStrip));
        OnPropertyChanged(nameof(IsDiscountDisplayTotalCard));
        OnPropertyChanged(nameof(IsBasketLeft));
        OnPropertyChanged(nameof(IsBasketRight));
        StatusMessage = "POS görünüm ayarları varsayılana döndü.";
    }

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
                StatusMessage = $"Geçersiz tutar: {rawItem}";
                return;
            }

            if (parsedAmount <= 0)
            {
                StatusMessage = $"Tutar sıfırdan büyük olmalı: {rawItem}";
                return;
            }

            parsedAmounts.Add(decimal.Truncate(parsedAmount));
        }

        if (parsedAmounts.Count == 0)
        {
            StatusMessage = "En az bir hızlı tutar girin.";
            return;
        }

        _settingsService.UpdateQuickAmounts(parsedAmounts);
        OnPropertyChanged(nameof(QuickAmounts));
        QuickAmountsText = BuildQuickAmountsText();
        StatusMessage = "Hızlı tutarlar güncellendi.";
        AppDialogService.ShowSaved("Hızlı tutar ayarları");
    }

    private void ResetQuickAmounts()
    {
        if (!AppDialogService.ShowResetConfirmation("Hızlı tutar"))
        {
            return;
        }

        _settingsService.ResetQuickAmounts();
        OnPropertyChanged(nameof(QuickAmounts));
        QuickAmountsText = BuildQuickAmountsText();
        StatusMessage = "Varsayılan hızlı tutarlar geri yüklendi.";
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
                StatusMessage = $"Desteklenmeyen ödeme tipi: {rawItem}";
                return;
            }

            methodKeys.Add(normalizedKey);
        }

        if (methodKeys.Count == 0)
        {
            StatusMessage = "En az bir ödeme tipi seçin.";
            return;
        }

        _settingsService.UpdatePaymentMethods(methodKeys);
        OnPropertyChanged(nameof(PaymentMethods));
        OnPropertyChanged(nameof(DefaultPaymentMethodOptions));
        PaymentMethodsText = BuildPaymentMethodsText();
        StatusMessage = "Ödeme tipleri güncellendi.";
        AppDialogService.ShowSaved("Ödeme yöntemi ayarları");
    }

    private void ResetPaymentMethods()
    {
        if (!AppDialogService.ShowResetConfirmation("Ödeme yöntemi"))
        {
            return;
        }

        _settingsService.ResetPaymentMethods();
        OnPropertyChanged(nameof(PaymentMethods));
        OnPropertyChanged(nameof(DefaultPaymentMethodOptions));
        PaymentMethodsText = BuildPaymentMethodsText();
        StatusMessage = "Varsayılan ödeme tipleri geri yüklendi.";
        DefaultPaymentMethod = _settingsService.DefaultPaymentMethod;
    }

    private void SaveGeneralSettings()
    {
        var normalizedDefaultPaymentMethod = PosSettingsService.NormalizePaymentMethodKey(DefaultPaymentMethod);

        if (normalizedDefaultPaymentMethod is null)
        {
            StatusMessage = $"Desteklenmeyen varsayılan ödeme tipi: {DefaultPaymentMethod}";
            return;
        }

        if (!_themeOptions.Any(x => x.Key == Theme))
        {
            StatusMessage = $"Desteklenmeyen tema: {Theme}";
            AppDialogService.ShowOperationFailed("Tema Geçersiz", $"'{Theme}' teması desteklenmiyor.", "SET-THEME-400");
            return;
        }

        _settingsService.UpdateGeneralSettings(
            normalizedDefaultPaymentMethod,
            Theme,
            ReceiptPrinterName);

        DefaultPaymentMethod = _settingsService.DefaultPaymentMethod;
        Theme = _settingsService.Theme;
        ReceiptPrinterName = _settingsService.ReceiptPrinterName;
        StatusMessage = "Genel ayarlar güncellendi.";
        AppDialogService.ShowSaved("Genel ayarlar");
        AppDialogService.ShowInfo("Tema Uygulandı", $"{ThemeDisplay} tema aktif edildi.");
    }

    private void ResetGeneralSettings()
    {
        if (!AppDialogService.ShowResetConfirmation("Genel"))
        {
            return;
        }

        _settingsService.ResetGeneralSettings();
        DefaultPaymentMethod = _settingsService.DefaultPaymentMethod;
        Theme = _settingsService.Theme;
        ReceiptPrinterName = _settingsService.ReceiptPrinterName;
        StatusMessage = "Genel ayarlar varsayılana döndü.";
    }

    private void SavePriceChangeSettings()
    {
        if (!_priceChangeModeOptions.Any(x => x.Key == PriceChangeMode))
        {
            StatusMessage = $"Desteklenmeyen fiyat değişim tipi: {PriceChangeMode}";
            return;
        }

        if (!decimal.TryParse(PriceChangeValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue) &&
            !decimal.TryParse(PriceChangeValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedValue))
        {
            StatusMessage = $"Geçersiz fiyat değişim değeri: {PriceChangeValue}";
            return;
        }

        _settingsService.UpdatePriceChangeSettings(PriceChangeMode, parsedValue, RequirePriceApproval);
        PriceChangeMode = _settingsService.PriceChangeMode;
        PriceChangeValue = _settingsService.PriceChangeValue.ToString("0.##", CultureInfo.InvariantCulture);
        RequirePriceApproval = _settingsService.RequirePriceApproval;
        StatusMessage = "Fiyat değişimi ayarları güncellendi.";
        AppDialogService.ShowSaved("Fiyat değişimi ayarları");
    }

    private void ResetPriceChangeSettings()
    {
        if (!AppDialogService.ShowResetConfirmation("Fiyat değişimi"))
        {
            return;
        }

        _settingsService.ResetPriceChangeSettings();
        PriceChangeMode = _settingsService.PriceChangeMode;
        PriceChangeValue = _settingsService.PriceChangeValue.ToString("0.##", CultureInfo.InvariantCulture);
        RequirePriceApproval = _settingsService.RequirePriceApproval;
        StatusMessage = "Fiyat değişimi ayarları varsayılana döndü.";
    }

    private void SaveLabelPrintSettings()
    {
        if (!LabelTemplateOptions.Contains(LabelTemplate))
        {
            StatusMessage = $"Desteklenmeyen etiket şablonu: {LabelTemplate}";
            AppDialogService.ShowOperationFailed("Etiket Şablonu Geçersiz", $"'{LabelTemplate}' şablonu desteklenmiyor.", "LBL-TPL-400");
            return;
        }

        if (!int.TryParse(LabelCopyCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCopyCount) &&
            !int.TryParse(LabelCopyCount, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsedCopyCount))
        {
            StatusMessage = $"Geçersiz etiket kopya sayısı: {LabelCopyCount}";
            AppDialogService.ShowOperationFailed("Kopya Sayısı Geçersiz", $"'{LabelCopyCount}' değeri sayı olmalı.", "LBL-CPY-400");
            return;
        }

        _settingsService.UpdateLabelPrintSettings(LabelTemplate, LabelPrinterName, parsedCopyCount, PrintBarcodeOnLabel);
        LabelTemplate = _settingsService.LabelTemplate;
        LabelPrinterName = _settingsService.LabelPrinterName;
        LabelCopyCount = _settingsService.LabelCopyCount.ToString(CultureInfo.InvariantCulture);
        PrintBarcodeOnLabel = _settingsService.PrintBarcodeOnLabel;
        StatusMessage = "Etiket basımı ayarları güncellendi.";
        AppDialogService.ShowSaved("Etiket basımı ayarları");
    }

    private void ResetLabelPrintSettings()
    {
        if (!AppDialogService.ShowResetConfirmation("Etiket basımı"))
        {
            return;
        }

        _settingsService.ResetLabelPrintSettings();
        LabelTemplate = _settingsService.LabelTemplate;
        LabelPrinterName = _settingsService.LabelPrinterName;
        LabelCopyCount = _settingsService.LabelCopyCount.ToString(CultureInfo.InvariantCulture);
        PrintBarcodeOnLabel = _settingsService.PrintBarcodeOnLabel;
        StatusMessage = "Etiket basımı ayarları varsayılana döndü.";
    }

    private void SaveQuickProductLayout()
    {
        if (!int.TryParse(QuickProductColumns, NumberStyles.Integer, CultureInfo.InvariantCulture, out var columns) &&
            !int.TryParse(QuickProductColumns, NumberStyles.Integer, CultureInfo.CurrentCulture, out columns))
        {
            StatusMessage = $"Geçersiz ürün sütun sayısı: {QuickProductColumns}";
            return;
        }

        if (!int.TryParse(QuickProductMinButtonHeight, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minHeight) &&
            !int.TryParse(QuickProductMinButtonHeight, NumberStyles.Integer, CultureInfo.CurrentCulture, out minHeight))
        {
            StatusMessage = $"Geçersiz min. yükseklik: {QuickProductMinButtonHeight}";
            return;
        }

        if (!int.TryParse(QuickProductButtonFontSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fontSize) &&
            !int.TryParse(QuickProductButtonFontSize, NumberStyles.Integer, CultureInfo.CurrentCulture, out fontSize))
        {
            StatusMessage = $"Geçersiz yazı boyutu: {QuickProductButtonFontSize}";
            return;
        }

        if (!int.TryParse(QuickProductCategoryColumns, NumberStyles.Integer, CultureInfo.InvariantCulture, out var catColumns) &&
            !int.TryParse(QuickProductCategoryColumns, NumberStyles.Integer, CultureInfo.CurrentCulture, out catColumns))
        {
            StatusMessage = $"Geçersiz kategori sütun sayısı: {QuickProductCategoryColumns}";
            return;
        }

        _settingsService.UpdateQuickProductLayout(columns, minHeight, fontSize, catColumns);
        QuickProductColumns = _settingsService.QuickProductColumns.ToString(CultureInfo.InvariantCulture);
        QuickProductMinButtonHeight = _settingsService.QuickProductMinButtonHeight.ToString(CultureInfo.InvariantCulture);
        QuickProductButtonFontSize = _settingsService.QuickProductButtonFontSize.ToString(CultureInfo.InvariantCulture);
        QuickProductCategoryColumns = _settingsService.QuickProductCategoryColumns.ToString(CultureInfo.InvariantCulture);
        StatusMessage = "Hızlı ürün düzeni güncellendi.";
        AppDialogService.ShowSaved("Hızlı ürün düzeni ayarları");
    }

    private void ResetQuickProductLayout()
    {
        if (!AppDialogService.ShowResetConfirmation("Hızlı ürün düzeni"))
        {
            return;
        }

        _settingsService.ResetQuickProductLayout();
        QuickProductColumns = _settingsService.QuickProductColumns.ToString(CultureInfo.InvariantCulture);
        QuickProductMinButtonHeight = _settingsService.QuickProductMinButtonHeight.ToString(CultureInfo.InvariantCulture);
        QuickProductButtonFontSize = _settingsService.QuickProductButtonFontSize.ToString(CultureInfo.InvariantCulture);
        QuickProductCategoryColumns = _settingsService.QuickProductCategoryColumns.ToString(CultureInfo.InvariantCulture);
        StatusMessage = "Hızlı ürün düzeni varsayılana döndü.";
    }

    private string BuildQuickAmountsText()
    {
        return string.Join(", ", _settingsService.QuickAmounts
            .Where(x => x.Value != "TOTAL")
            .Select(x => x.Value));
    }

    private string BuildPaymentMethodsText()
    {
        return string.Join(", ", _settingsService.PaymentMethods.Select(x => x.Title));
    }

    private string ResolvePaymentMethodTitle(string key)
    {
        return _settingsService.PaymentMethods.FirstOrDefault(x => x.Key == key)?.Title ?? key;
    }

    private static string ResolveThemeTitle(string key)
    {
        return _themeOptions.FirstOrDefault(x => x.Key == key)?.Title ?? key;
    }

    private static string ResolvePriceChangeModeTitle(string key)
    {
        return _priceChangeModeOptions.FirstOrDefault(x => x.Key == key)?.Title ?? key;
    }

    public sealed record SelectionOption(string Key, string Title);
}


