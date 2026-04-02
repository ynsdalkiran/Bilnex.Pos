using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Bilnex.Pos.Models;

namespace Bilnex.Pos.Services;

public sealed class PosSettingsService
{
    private static readonly Lazy<PosSettingsService> _current = new(() => new PosSettingsService());
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _settingsFilePath;
    private string _defaultPaymentMethod = "Cash";
    private string _theme = "Dark";
    private string _receiptPrinterName = "Varsayılan Yazıcı";
    private string _priceChangeMode = "Percentage";
    private decimal _priceChangeValue;
    private bool _requirePriceApproval = true;
    private string _labelTemplate = "50x30";
    private string _labelPrinterName = "Varsayılan Etiket Yazıcısı";
    private int _labelCopyCount = 1;
    private bool _printBarcodeOnLabel = true;
    private string _storeName = "Bilnex POS";
    private string _cashierName = "Demo Kasiyer";
    private string _terminalLabel = "Ana Kasa";
    private int _quickProductColumns = 2;
    private int _quickProductMinButtonHeight = 72;
    private int _quickProductButtonFontSize = 16;
    private int _quickProductCategoryColumns = 5;

    private PosSettingsService()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bilnex.Pos");
        Directory.CreateDirectory(appDataDirectory);
        _settingsFilePath = Path.Combine(appDataDirectory, "pos-settings.json");
        QuickAmounts = new ObservableCollection<QuickAmountOption>();
        PaymentMethods = new ObservableCollection<PaymentMethodOption>();
        LoadSettings();
    }

    public static PosSettingsService Current => _current.Value;

    public event EventHandler? SettingsChanged;

    public ObservableCollection<QuickAmountOption> QuickAmounts { get; }

    public ObservableCollection<PaymentMethodOption> PaymentMethods { get; }

    public string DefaultPaymentMethod
    {
        get => _defaultPaymentMethod;
        private set => _defaultPaymentMethod = value;
    }

    public string Theme
    {
        get => _theme;
        private set => _theme = value;
    }

    public string ReceiptPrinterName
    {
        get => _receiptPrinterName;
        private set => _receiptPrinterName = value;
    }

    public string PriceChangeMode
    {
        get => _priceChangeMode;
        private set => _priceChangeMode = value;
    }

    public decimal PriceChangeValue
    {
        get => _priceChangeValue;
        private set => _priceChangeValue = value;
    }

    public bool RequirePriceApproval
    {
        get => _requirePriceApproval;
        private set => _requirePriceApproval = value;
    }

    public string LabelTemplate
    {
        get => _labelTemplate;
        private set => _labelTemplate = value;
    }

    public string LabelPrinterName
    {
        get => _labelPrinterName;
        private set => _labelPrinterName = value;
    }

    public int LabelCopyCount
    {
        get => _labelCopyCount;
        private set => _labelCopyCount = value;
    }

    public bool PrintBarcodeOnLabel
    {
        get => _printBarcodeOnLabel;
        private set => _printBarcodeOnLabel = value;
    }

    public string StoreName
    {
        get => _storeName;
        private set => _storeName = value;
    }

    public string CashierName
    {
        get => _cashierName;
        private set => _cashierName = value;
    }

    public string TerminalLabel
    {
        get => _terminalLabel;
        private set => _terminalLabel = value;
    }

    public int QuickProductColumns
    {
        get => _quickProductColumns;
        private set => _quickProductColumns = value;
    }

    public int QuickProductMinButtonHeight
    {
        get => _quickProductMinButtonHeight;
        private set => _quickProductMinButtonHeight = value;
    }

    public int QuickProductButtonFontSize
    {
        get => _quickProductButtonFontSize;
        private set => _quickProductButtonFontSize = value;
    }

    public int QuickProductCategoryColumns
    {
        get => _quickProductCategoryColumns;
        private set => _quickProductCategoryColumns = value;
    }

    public void UpdateQuickProductLayout(int columns, int minButtonHeight, int buttonFontSize, int categoryColumns)
    {
        ApplyQuickProductLayout(columns, minButtonHeight, buttonFontSize, categoryColumns);
        SaveSettings();
    }

    public void ResetQuickProductLayout()
    {
        ApplyQuickProductLayout(2, 72, 16, 5);
        SaveSettings();
    }

    public void UpdateQuickAmounts(IEnumerable<decimal> amounts)
    {
        ApplyQuickAmounts(amounts);
        SaveSettings();
    }

    public void ResetQuickAmounts()
    {
        ApplyQuickAmounts(new[] { 50m, 100m, 200m, 500m, 1000m });
        SaveSettings();
    }

    public void UpdatePaymentMethods(IEnumerable<string> methodKeys)
    {
        ApplyPaymentMethods(methodKeys);
        SaveSettings();
    }

    public void ResetPaymentMethods()
    {
        ApplyPaymentMethods(new[] { "Cash", "Card" });
        SaveSettings();
    }

    public void UpdateGeneralSettings(string defaultPaymentMethod, string theme, string receiptPrinterName)
    {
        ApplyGeneralSettings(defaultPaymentMethod, theme, receiptPrinterName);
        SaveSettings();
    }

    public void ResetGeneralSettings()
    {
        ApplyGeneralSettings("Cash", "Dark", "Varsayılan Yazıcı");
        SaveSettings();
    }

    public void UpdatePriceChangeSettings(string mode, decimal value, bool requireApproval)
    {
        ApplyPriceChangeSettings(mode, value, requireApproval);
        SaveSettings();
    }

    public void ResetPriceChangeSettings()
    {
        ApplyPriceChangeSettings("Percentage", 0m, true);
        SaveSettings();
    }

    public void UpdateLabelPrintSettings(string template, string printerName, int copyCount, bool printBarcode)
    {
        ApplyLabelPrintSettings(template, printerName, copyCount, printBarcode);
        SaveSettings();
    }

    public void ResetLabelPrintSettings()
    {
        ApplyLabelPrintSettings("50x30", "Varsayılan Etiket Yazıcısı", 1, true);
        SaveSettings();
    }

    public static string? NormalizePaymentMethodKey(string rawValue)
    {
        return rawValue.Trim().ToLowerInvariant() switch
        {
            "cash" => "Cash",
            "nakit" => "Cash",
            "card" => "Card",
            "kart" => "Card",
            _ => null
        };
    }

    private void LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            ApplyDefaults();
            SaveSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<PosSettingsFile>(json, _jsonOptions);

            if (settings is null)
            {
                ApplyDefaults();
                SaveSettings();
                return;
            }

            ApplyQuickAmounts(settings.QuickAmounts);
            ApplyPaymentMethods(settings.PaymentMethods);
            ApplyGeneralSettings(
                settings.DefaultPaymentMethod,
                settings.Theme,
                settings.ReceiptPrinterName);
            ApplyPriceChangeSettings(
                settings.PriceChangeMode,
                settings.PriceChangeValue,
                settings.RequirePriceApproval);
            ApplyLabelPrintSettings(
                settings.LabelTemplate,
                settings.LabelPrinterName,
                settings.LabelCopyCount,
                settings.PrintBarcodeOnLabel);
            ApplyQuickProductLayout(
                settings.QuickProductColumns,
                settings.QuickProductMinButtonHeight,
                settings.QuickProductButtonFontSize,
                settings.QuickProductCategoryColumns);
            SaveSettings();
        }
        catch
        {
            ApplyDefaults();
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        var settings = new PosSettingsFile
        {
            QuickAmounts = QuickAmounts
                .Where(x => x.Value != "TOTAL")
                .Select(x => decimal.Parse(x.Value, CultureInfo.InvariantCulture))
                .ToList(),
            PaymentMethods = PaymentMethods
                .Select(x => x.Key)
                .ToList(),
            DefaultPaymentMethod = DefaultPaymentMethod,
            Theme = Theme,
            ReceiptPrinterName = ReceiptPrinterName,
            PriceChangeMode = PriceChangeMode,
            PriceChangeValue = PriceChangeValue,
            RequirePriceApproval = RequirePriceApproval,
            LabelTemplate = LabelTemplate,
            LabelPrinterName = LabelPrinterName,
            LabelCopyCount = LabelCopyCount,
            PrintBarcodeOnLabel = PrintBarcodeOnLabel,
            QuickProductColumns = QuickProductColumns,
            QuickProductMinButtonHeight = QuickProductMinButtonHeight,
            QuickProductButtonFontSize = QuickProductButtonFontSize,
            QuickProductCategoryColumns = QuickProductCategoryColumns
        };

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsFilePath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyDefaults()
    {
        ApplyQuickAmounts(new[] { 50m, 100m, 200m, 500m, 1000m });
        ApplyPaymentMethods(new[] { "Cash", "Card" });
        ApplyGeneralSettings("Cash", "Dark", "Varsayılan Yazıcı");
        ApplyPriceChangeSettings("Percentage", 0m, true);
        ApplyLabelPrintSettings("50x30", "Varsayılan Etiket Yazıcısı", 1, true);
        ApplyQuickProductLayout(2, 72, 16, 5);
    }

    private void ApplyQuickAmounts(IEnumerable<decimal> amounts)
    {
        QuickAmounts.Clear();

        foreach (var amount in amounts.Distinct().OrderBy(x => x))
        {
            QuickAmounts.Add(new QuickAmountOption(
                amount.ToString("0", CultureInfo.InvariantCulture),
                amount.ToString("0", CultureInfo.InvariantCulture)));
        }

        if (QuickAmounts.Count == 0)
        {
            foreach (var amount in new[] { 50m, 100m, 200m, 500m, 1000m })
            {
                QuickAmounts.Add(new QuickAmountOption(
                    amount.ToString("0", CultureInfo.InvariantCulture),
                    amount.ToString("0", CultureInfo.InvariantCulture)));
            }
        }

        QuickAmounts.Add(new QuickAmountOption("Tüm Tutar", "TOTAL"));
    }

    private void ApplyPaymentMethods(IEnumerable<string> methodKeys)
    {
        PaymentMethods.Clear();

        foreach (var methodKey in methodKeys
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Select(NormalizePaymentMethodKey)
                     .Where(x => x is not null))
        {
            PaymentMethods.Add(methodKey! switch
            {
                "Cash" => new PaymentMethodOption("Cash", "Nakit"),
                "Card" => new PaymentMethodOption("Card", "Kart"),
                _ => throw new InvalidOperationException("Unsupported payment method.")
            });
        }

        if (PaymentMethods.Count == 0)
        {
            PaymentMethods.Add(new PaymentMethodOption("Cash", "Nakit"));
            PaymentMethods.Add(new PaymentMethodOption("Card", "Kart"));
        }

        if (!PaymentMethods.Any(x => x.Key == DefaultPaymentMethod))
        {
            DefaultPaymentMethod = PaymentMethods.First().Key;
        }
    }

    private void ApplyGeneralSettings(string? defaultPaymentMethod, string? theme, string? receiptPrinterName)
    {
        var normalizedDefaultPaymentMethod = NormalizePaymentMethodKey(defaultPaymentMethod ?? string.Empty);
        DefaultPaymentMethod = normalizedDefaultPaymentMethod is not null &&
                               PaymentMethods.Any(x => x.Key == normalizedDefaultPaymentMethod)
            ? normalizedDefaultPaymentMethod
            : PaymentMethods.FirstOrDefault()?.Key ?? "Cash";

        Theme = NormalizeTheme(theme);

        var normalizedPrinterName = receiptPrinterName?.Trim();
        ReceiptPrinterName = string.IsNullOrWhiteSpace(normalizedPrinterName) ||
                             string.Equals(normalizedPrinterName, "Default Printer", StringComparison.OrdinalIgnoreCase)
            ? "Varsayılan Yazıcı"
            : normalizedPrinterName;
    }

    private static string NormalizeTheme(string? theme)
    {
        return theme?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            _ => "Dark"
        };
    }

    private void ApplyPriceChangeSettings(string? mode, decimal value, bool requireApproval)
    {
        PriceChangeMode = NormalizePriceChangeMode(mode);
        PriceChangeValue = decimal.Round(Math.Max(-100m, Math.Min(100000m, value)), 2);
        RequirePriceApproval = requireApproval;
    }

    private void ApplyLabelPrintSettings(string? template, string? printerName, int copyCount, bool printBarcode)
    {
        LabelTemplate = NormalizeLabelTemplate(template);

        var normalizedPrinterName = printerName?.Trim();
        LabelPrinterName = string.IsNullOrWhiteSpace(normalizedPrinterName) ||
                           string.Equals(normalizedPrinterName, "Default Label Printer", StringComparison.OrdinalIgnoreCase)
            ? "Varsayılan Etiket Yazıcısı"
            : normalizedPrinterName;

        LabelCopyCount = Math.Max(1, Math.Min(100, copyCount));
        PrintBarcodeOnLabel = printBarcode;
    }

    private static string NormalizePriceChangeMode(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            "fixed" => "Fixed",
            _ => "Percentage"
        };
    }

    private static string NormalizeLabelTemplate(string? template)
    {
        return template?.Trim() switch
        {
            "58x40" => "58x40",
            "100x70" => "100x70",
            _ => "50x30"
        };
    }

    private void ApplyQuickProductLayout(int columns, int minButtonHeight, int buttonFontSize, int categoryColumns)
    {
        QuickProductColumns = Math.Max(1, Math.Min(6, columns));
        QuickProductMinButtonHeight = Math.Max(48, Math.Min(200, minButtonHeight));
        QuickProductButtonFontSize = Math.Max(11, Math.Min(28, buttonFontSize));
        QuickProductCategoryColumns = Math.Max(1, Math.Min(10, categoryColumns));
    }

    private sealed class PosSettingsFile
    {
        public List<decimal> QuickAmounts { get; set; } = new();

        public List<string> PaymentMethods { get; set; } = new();

        public string DefaultPaymentMethod { get; set; } = "Cash";

        public string Theme { get; set; } = "Dark";

        public string ReceiptPrinterName { get; set; } = "Varsayılan Yazıcı";

        public string PriceChangeMode { get; set; } = "Percentage";

        public decimal PriceChangeValue { get; set; }

        public bool RequirePriceApproval { get; set; } = true;

        public string LabelTemplate { get; set; } = "50x30";

        public string LabelPrinterName { get; set; } = "Varsayılan Etiket Yazıcısı";

        public int LabelCopyCount { get; set; } = 1;

        public bool PrintBarcodeOnLabel { get; set; } = true;

        public int QuickProductColumns { get; set; } = 2;

        public int QuickProductMinButtonHeight { get; set; } = 72;

        public int QuickProductButtonFontSize { get; set; } = 16;

        public int QuickProductCategoryColumns { get; set; } = 5;
    }
}
