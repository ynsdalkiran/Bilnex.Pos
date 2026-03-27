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
    private string _receiptPrinterName = "Default Printer";

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
        ApplyGeneralSettings("Cash", "Dark", "Default Printer");
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
            ReceiptPrinterName = ReceiptPrinterName
        };

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsFilePath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyDefaults()
    {
        ApplyQuickAmounts(new[] { 50m, 100m, 200m, 500m, 1000m });
        ApplyPaymentMethods(new[] { "Cash", "Card" });
        ApplyGeneralSettings("Cash", "Dark", "Default Printer");
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

        QuickAmounts.Add(new QuickAmountOption("Tum Tutar", "TOTAL"));
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
        ReceiptPrinterName = string.IsNullOrWhiteSpace(receiptPrinterName)
            ? "Default Printer"
            : receiptPrinterName.Trim();
    }

    private static string NormalizeTheme(string? theme)
    {
        return theme?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            _ => "Dark"
        };
    }

    private sealed class PosSettingsFile
    {
        public List<decimal> QuickAmounts { get; set; } = new();

        public List<string> PaymentMethods { get; set; } = new();

        public string DefaultPaymentMethod { get; set; } = "Cash";

        public string Theme { get; set; } = "Dark";

        public string ReceiptPrinterName { get; set; } = "Default Printer";
    }
}
