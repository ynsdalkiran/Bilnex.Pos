using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Bilnex.Pos.Commands;
using Bilnex.Pos.Models;
using Bilnex.Pos.Services;
using Bilnex.Pos.States;
using Bilnex.Pos.ViewModels.Base;

namespace Bilnex.Pos.ViewModels;

public sealed class PosViewModel : ViewModelBase
{
    private const int MinimumScannerBarcodeLength = 3;
    private const int ScannerThresholdMilliseconds = 50;
    private const int ScannerCompleteDelayMilliseconds = 90;
    private const int MaxHistoryItems = 8;
    private const string AllQuickProductCategoryKey = "all";
    private const string QuickProductsTabKey = "quick";
    private const string ProductActionsTabKey = "product";
    private const string ReceiptActionsTabKey = "receipt";
    private const string DiscountInputRateTarget = "rate";
    private const string DiscountInputAmountTarget = "amount";

    private readonly Dictionary<string, decimal> _productPrices = new()
    {
        ["Su"] = 10m,
        ["Ekmek"] = 15m,
        ["Süt"] = 35m,
        ["Kola"] = 40m,
        ["Ayran"] = 20m,
        ["Maden Suyu"] = 12m,
        ["Poğaça"] = 18m,
        ["Simit"] = 17m,
        ["Yoğurt"] = 45m,
        ["Peynir"] = 95m,
        ["Cips"] = 32m,
        ["Çikolata"] = 28m,
        ["Bisküvi"] = 22m
    };

    private readonly Dictionary<string, string> _barcodeProductMap = new()
    {
        ["111"] = "Su",
        ["222"] = "Ekmek",
        ["333"] = "Süt",
        ["444"] = "Kola",
        ["555"] = "Ayran",
        ["666"] = "Maden Suyu",
        ["777"] = "Poğaça",
        ["888"] = "Simit",
        ["999"] = "Yoğurt"
    };

    private readonly List<QuickProductItem> _allQuickProducts;
    private readonly List<PosActionItem> _allActionItems;

    private readonly PosStateManager _stateManager;
    private readonly DispatcherTimer _scannerCompleteTimer;
    private readonly PosSettingsService _settingsService;

    private Receipt _currentReceipt;
    private BasketItem? _selectedItem;
    private string _barcode = string.Empty;
    private bool _isScannerInputActive;
    private string _lastScannedProduct = "Henüz ürün okutulmadı";
    private string _lastInvalidBarcode = string.Empty;
    private bool _isPaymentScreenOpen;
    private bool _isReceiptDiscountScreenOpen;
    private bool _isHeldSalesScreenOpen;
    private bool _isReceiptHistoryScreenOpen;
    private string _paymentMethod = "Cash";
    private string _receivedAmountText = string.Empty;
    private string _receiptDiscountRateText = string.Empty;
    private string _receiptDiscountAmountText = string.Empty;
    private string _receiptDiscountInputTarget = DiscountInputRateTarget;
    private string _receiptDiscountLabel = string.Empty;
    private ReceiptRoundMode _receiptRoundMode = ReceiptRoundMode.None;
    private DateTime? _lastBarcodeCharacterAtUtc;
    private QuickProductCategoryItem? _selectedQuickProductCategory;
    private PosActionTabItem? _selectedActionTab;
    private bool _isUpdatingReceiptDiscountInputs;

    public PosViewModel()
    {
        _stateManager = new PosStateManager();
        _settingsService = PosSettingsService.Current;
        _scannerCompleteTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ScannerCompleteDelayMilliseconds)
        };
        _scannerCompleteTimer.Tick += ScannerCompleteTimer_Tick;

        _currentReceipt = CreateReceipt();
        SubscribeToReceipt(_currentReceipt);

        _allQuickProducts =
        [
            new("Su", "icecek"),
            new("Kola", "icecek"),
            new("Ayran", "icecek"),
            new("Maden Suyu", "icecek"),
            new("Ekmek", "firin"),
            new("Poğaça", "firin"),
            new("Simit", "firin"),
            new("Süt", "sut"),
            new("Yoğurt", "sut"),
            new("Peynir", "sut"),
            new("Cips", "atistirmalik"),
            new("Çikolata", "atistirmalik"),
            new("Bisküvi", "atistirmalik")
        ];

        QuickProductCategories = new ObservableCollection<QuickProductCategoryItem>
        {
            new(AllQuickProductCategoryKey, "Tümü"),
            new("icecek", "İçecekler"),
            new("firin", "Fırın"),
            new("sut", "Süt Ürünleri"),
            new("atistirmalik", "Atıştırmalık")
        };
        QuickProducts = new ObservableCollection<QuickProductItem>();
        ActionTabs = new ObservableCollection<PosActionTabItem>
        {
            new(QuickProductsTabKey, "Hızlı Ürünler"),
            new(ProductActionsTabKey, "Ürün İşlemleri"),
            new(ReceiptActionsTabKey, "Fiş İşlemleri")
        };
        VisibleActionItems = new ObservableCollection<PosActionItem>();
        ReceiptDiscountPresets = new ObservableCollection<ReceiptDiscountPresetItem>
        {
            new("Özel Müşteri %5", ReceiptDiscountPresetKind.Percentage, 5m),
            new("Personel %10", ReceiptDiscountPresetKind.Percentage, 10m),
            new("Sadakat %7,5", ReceiptDiscountPresetKind.Percentage, 7.5m),
            new("Kampanya 20 TL", ReceiptDiscountPresetKind.Amount, 20m)
        };
        BarcodeHistory = new ObservableCollection<BarcodeHistoryItem>();
        HeldSales = new ObservableCollection<HeldSaleItem>();
        CompletedReceipts = new ObservableCollection<Receipt>();
        _allActionItems =
        [
            new("line-discount-fixed", ProductActionsTabKey, "Ürüne İndirim", "Tutar indir"),
            new("line-discount-rate", ProductActionsTabKey, "Yüzde İndir", "% indirim uygula"),
            new("line-price-change", ProductActionsTabKey, "Fiyat Değiştir", "Seçili satırı düzenle"),
            new("line-quantity", ProductActionsTabKey, "Miktar Değiştir", "Adedi güncelle"),
            new("line-remove", ProductActionsTabKey, "Satır Sil", "Seçili ürünü kaldır"),
            new("line-return", ProductActionsTabKey, "Ürün İade", "Satır için iade işlemi"),
            new("receipt-discount", ReceiptActionsTabKey, "Fiş İndirimi", "Tutar, yüzde ve yuvarlama"),
            new("receipt-customer", ReceiptActionsTabKey, "Müşteri Seç", "Cari ata"),
            new("receipt-return", ReceiptActionsTabKey, "İade İşlemi", "Fiş iadesi başlat"),
            new("receipt-suspend", ReceiptActionsTabKey, "Askıya Al", "Sepeti beklet"),
            new("receipt-held-list", ReceiptActionsTabKey, "Askı Listesi", "Bekleyen fişleri aç"),
            new("receipt-history", ReceiptActionsTabKey, "Fiş Geçmişi", "Tamamlananları aç")
        ];

        AddProductCommand = new RelayCommand(parameter =>
        {
            if (parameter is string productName)
            {
                AddProduct(productName);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Barcode))
            {
                AddProductByBarcode(Barcode);
            }
        });

        DecreaseQuantityCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is BasketItem item)
                {
                    DecreaseItemQuantity(item);
                }
            },
            parameter => parameter is BasketItem);

        IncreaseQuantityCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is BasketItem item)
                {
                    IncreaseItemQuantity(item);
                }
            },
            parameter => parameter is BasketItem);

        RemoveItemCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is BasketItem item)
                {
                    RemoveItem(item);
                }
            },
            parameter => parameter is BasketItem);

        CashPaymentCommand = new RelayCommand(() => OpenPaymentScreen("Cash"), () => BasketItems.Count > 0);
        CardPaymentCommand = new RelayCommand(() => OpenPaymentScreen("Card"), () => BasketItems.Count > 0);
        ReceivePaymentCommand = new RelayCommand(OpenDefaultPaymentScreen, () => BasketItems.Count > 0);
        OpenPaymentMethodCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string paymentMethod)
                {
                    OpenPaymentScreen(paymentMethod);
                }
            },
            parameter => parameter is string && BasketItems.Count > 0);
        SelectPaymentMethodCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string paymentMethod)
                {
                    SelectPaymentMethod(paymentMethod);
                }
            },
            parameter => parameter is string);
        SetQuickReceivedAmountCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string amountKey)
                {
                    SetQuickReceivedAmount(amountKey);
                }
            },
            parameter => parameter is string && IsCashPayment);
        CompletePaymentCommand = new RelayCommand(ConfirmPayment, CanConfirmPayment);
        CancelPaymentCommand = new RelayCommand(CancelPaymentScreen);
        SuspendSaleCommand = new RelayCommand(SuspendCurrentSale, () => BasketItems.Count > 0);
        OpenHeldSalesCommand = new RelayCommand(OpenHeldSalesScreen, () => HeldSales.Count > 0);
        CloseHeldSalesCommand = new RelayCommand(CloseHeldSalesScreen);
        OpenReceiptHistoryCommand = new RelayCommand(OpenReceiptHistoryScreen, () => CompletedReceipts.Count > 0);
        CloseReceiptHistoryCommand = new RelayCommand(CloseReceiptHistoryScreen);
        PrintReceiptCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is Receipt receipt)
                {
                    PrintReceipt(receipt);
                }
            },
            parameter => parameter is Receipt);
        RestoreCompletedReceiptCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is Receipt receipt)
                {
                    RestoreCompletedReceipt(receipt);
                }
            },
            parameter => parameter is Receipt);
        RecallHeldSaleCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is HeldSaleItem heldSale)
                {
                    RecallHeldSale(heldSale);
                }
            },
            parameter => parameter is HeldSaleItem);
        SelectQuickProductCategoryCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is QuickProductCategoryItem category)
                {
                    SelectQuickProductCategory(category);
                }
            },
            parameter => parameter is QuickProductCategoryItem);
        SelectActionTabCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is PosActionTabItem tab)
                {
                    SelectActionTab(tab);
                }
            },
            parameter => parameter is PosActionTabItem);
        TriggerPosActionCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is PosActionItem actionItem)
                {
                    TriggerPosAction(actionItem);
                }
            },
            parameter => parameter is PosActionItem);
        OpenReceiptDiscountCommand = new RelayCommand(OpenReceiptDiscountScreen, () => BasketItems.Count > 0);
        CloseReceiptDiscountCommand = new RelayCommand(CloseReceiptDiscountScreen);
        ApplyReceiptDiscountCommand = new RelayCommand(ApplyReceiptDiscount, () => BasketItems.Count > 0);
        SelectReceiptDiscountPresetCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is ReceiptDiscountPresetItem preset)
                {
                    SelectReceiptDiscountPreset(preset);
                }
            },
            parameter => parameter is ReceiptDiscountPresetItem);
        SetReceiptDiscountInputTargetCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string target)
                {
                    ReceiptDiscountInputTarget = target;
                }
            },
            parameter => parameter is string);
        AppendReceiptDiscountNumpadCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string value)
                {
                    AppendReceiptDiscountInput(value);
                }
            },
            parameter => parameter is string);
        BackspaceReceiptDiscountNumpadCommand = new RelayCommand(BackspaceReceiptDiscountInput);
        ClearReceiptDiscountNumpadCommand = new RelayCommand(ClearReceiptDiscountInput);
        SetReceiptRoundModeCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string roundMode)
                {
                    SetReceiptRoundMode(roundMode);
                }
            },
            parameter => parameter is string);

        SelectQuickProductCategory(QuickProductCategories.First());
        SelectActionTab(ActionTabs.First());
        SetIdleState();
    }

    public Receipt CurrentReceipt
    {
        get => _currentReceipt;
        private set
        {
            if (ReferenceEquals(_currentReceipt, value))
            {
                return;
            }

            UnsubscribeFromReceipt(_currentReceipt);
            _currentReceipt = value;
            SubscribeToReceipt(_currentReceipt);

            OnPropertyChanged();
            OnPropertyChanged(nameof(BasketItems));
            OnPropertyChanged(nameof(ReceiptNo));
            OnPropertyChanged(nameof(ReceiptDiscountSubtotal));
            OnPropertyChanged(nameof(ReceiptDiscountValue));
            OnPropertyChanged(nameof(ReceiptDiscountRoundAdjustment));
            OnPropertyChanged(nameof(ReceiptDiscountPreviewTotal));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(ChangeAmountText));
            RaiseCommandStates();
        }
    }

    public ObservableCollection<BasketItem> BasketItems => CurrentReceipt.Items;

    public ObservableCollection<QuickProductItem> QuickProducts { get; }

    public ObservableCollection<QuickProductCategoryItem> QuickProductCategories { get; }

    public ObservableCollection<PosActionTabItem> ActionTabs { get; }

    public ObservableCollection<PosActionItem> VisibleActionItems { get; }

    public ObservableCollection<ReceiptDiscountPresetItem> ReceiptDiscountPresets { get; }

    public ObservableCollection<BarcodeHistoryItem> BarcodeHistory { get; }

    public ObservableCollection<HeldSaleItem> HeldSales { get; }

    public ObservableCollection<Receipt> CompletedReceipts { get; }

    public ObservableCollection<QuickAmountOption> QuickAmounts => _settingsService.QuickAmounts;

    public ObservableCollection<PaymentMethodOption> PaymentMethods => _settingsService.PaymentMethods;

    public ICommand AddProductCommand { get; }

    public ICommand DecreaseQuantityCommand { get; }

    public ICommand IncreaseQuantityCommand { get; }

    public ICommand RemoveItemCommand { get; }

    public ICommand CashPaymentCommand { get; }

    public ICommand CardPaymentCommand { get; }

    public ICommand ReceivePaymentCommand { get; }

    public ICommand OpenPaymentMethodCommand { get; }

    public ICommand SelectPaymentMethodCommand { get; }

    public ICommand SetQuickReceivedAmountCommand { get; }

    public ICommand CompletePaymentCommand { get; }

    public ICommand CancelPaymentCommand { get; }

    public ICommand SuspendSaleCommand { get; }

    public ICommand OpenHeldSalesCommand { get; }

    public ICommand CloseHeldSalesCommand { get; }

    public ICommand OpenReceiptHistoryCommand { get; }

    public ICommand CloseReceiptHistoryCommand { get; }

    public ICommand PrintReceiptCommand { get; }

    public ICommand RestoreCompletedReceiptCommand { get; }

    public ICommand RecallHeldSaleCommand { get; }

    public ICommand SelectQuickProductCategoryCommand { get; }

    public ICommand SelectActionTabCommand { get; }

    public ICommand TriggerPosActionCommand { get; }

    public ICommand OpenReceiptDiscountCommand { get; }

    public ICommand CloseReceiptDiscountCommand { get; }

    public ICommand ApplyReceiptDiscountCommand { get; }

    public ICommand SelectReceiptDiscountPresetCommand { get; }

    public ICommand SetReceiptDiscountInputTargetCommand { get; }

    public ICommand AppendReceiptDiscountNumpadCommand { get; }

    public ICommand BackspaceReceiptDiscountNumpadCommand { get; }

    public ICommand ClearReceiptDiscountNumpadCommand { get; }

    public ICommand SetReceiptRoundModeCommand { get; }

    public BasketItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RefreshActionItemStates();
            }
        }
    }

    public string ReceiptNo => CurrentReceipt.ReceiptNo;

    public QuickProductCategoryItem? SelectedQuickProductCategory
    {
        get => _selectedQuickProductCategory;
        private set => SetProperty(ref _selectedQuickProductCategory, value);
    }

    public PosActionTabItem? SelectedActionTab
    {
        get => _selectedActionTab;
        private set
        {
            if (SetProperty(ref _selectedActionTab, value))
            {
                OnPropertyChanged(nameof(IsQuickProductsTabSelected));
                OnPropertyChanged(nameof(IsActionItemsTabSelected));
            }
        }
    }

    public bool IsQuickProductsTabSelected => SelectedActionTab?.Key == QuickProductsTabKey;

    public bool IsActionItemsTabSelected => !IsQuickProductsTabSelected;

    public bool IsReceiptDiscountScreenOpen
    {
        get => _isReceiptDiscountScreenOpen;
        private set => SetProperty(ref _isReceiptDiscountScreenOpen, value);
    }

    public string ReceiptDiscountRateText
    {
        get => _receiptDiscountRateText;
        set
        {
            if (!SetProperty(ref _receiptDiscountRateText, value))
            {
                return;
            }

            if (_isUpdatingReceiptDiscountInputs)
            {
                return;
            }

            _isUpdatingReceiptDiscountInputs = true;
            ReceiptDiscountAmountText = string.Empty;
            _isUpdatingReceiptDiscountInputs = false;
            ReceiptDiscountLabel = "Manuel yüzde indirimi";
            NotifyReceiptDiscountPreviewChanged();
        }
    }

    public string ReceiptDiscountAmountText
    {
        get => _receiptDiscountAmountText;
        set
        {
            if (!SetProperty(ref _receiptDiscountAmountText, value))
            {
                return;
            }

            if (_isUpdatingReceiptDiscountInputs)
            {
                return;
            }

            _isUpdatingReceiptDiscountInputs = true;
            ReceiptDiscountRateText = string.Empty;
            _isUpdatingReceiptDiscountInputs = false;
            ReceiptDiscountLabel = "Manuel tutar indirimi";
            NotifyReceiptDiscountPreviewChanged();
        }
    }

    public string ReceiptDiscountInputTarget
    {
        get => _receiptDiscountInputTarget;
        private set
        {
            if (SetProperty(ref _receiptDiscountInputTarget, value))
            {
                OnPropertyChanged(nameof(ReceiptDiscountInputTargetText));
            }
        }
    }

    public string ReceiptDiscountLabel
    {
        get => _receiptDiscountLabel;
        private set => SetProperty(ref _receiptDiscountLabel, value);
    }

    public ReceiptRoundMode ReceiptRoundMode
    {
        get => _receiptRoundMode;
        private set
        {
            if (SetProperty(ref _receiptRoundMode, value))
            {
                OnPropertyChanged(nameof(ReceiptRoundModeText));
                NotifyReceiptDiscountPreviewChanged();
            }
        }
    }

    public decimal ReceiptDiscountSubtotal => CurrentReceipt.SubtotalAmount;

    public decimal ReceiptDiscountValue => CalculateReceiptDiscountValue();

    public decimal ReceiptDiscountRoundAdjustment => CalculateReceiptRoundAdjustment();

    public decimal ReceiptDiscountPreviewTotal => Math.Max(0m, ReceiptDiscountSubtotal - ReceiptDiscountValue + ReceiptDiscountRoundAdjustment);

    public string ReceiptDiscountInputTargetText => ReceiptDiscountInputTarget == DiscountInputAmountTarget
        ? "Tutar indirimi"
        : "Yüzde indirimi";

    public string ReceiptRoundModeText => ReceiptRoundMode switch
    {
        ReceiptRoundMode.Up => "Yukarı yuvarla",
        ReceiptRoundMode.Down => "Aşağı yuvarla",
        _ => "Yuvarlama yok"
    };

    public string Barcode
    {
        get => _barcode;
        set => SetProperty(ref _barcode, value);
    }

    public decimal TotalAmount => CurrentReceipt.TotalAmount;

    public string ItemCount => $"{BasketItems.Sum(x => x.Quantity)} items";

    public bool IsScannerInputActive
    {
        get => _isScannerInputActive;
        private set
        {
            if (SetProperty(ref _isScannerInputActive, value))
            {
                OnPropertyChanged(nameof(ScannerStatusText));
            }
        }
    }

    public string ScannerStatusText => IsScannerInputActive ? "Scanner input detected" : "Manual input mode";

    public string LastScannedProduct
    {
        get => _lastScannedProduct;
        private set => SetProperty(ref _lastScannedProduct, value);
    }

    public string LastInvalidBarcode
    {
        get => _lastInvalidBarcode;
        private set => SetProperty(ref _lastInvalidBarcode, value);
    }

    public bool IsPaymentScreenOpen
    {
        get => _isPaymentScreenOpen;
        private set => SetProperty(ref _isPaymentScreenOpen, value);
    }

    public bool IsHeldSalesScreenOpen
    {
        get => _isHeldSalesScreenOpen;
        private set => SetProperty(ref _isHeldSalesScreenOpen, value);
    }

    public bool IsReceiptHistoryScreenOpen
    {
        get => _isReceiptHistoryScreenOpen;
        private set => SetProperty(ref _isReceiptHistoryScreenOpen, value);
    }

    public string PaymentMethod
    {
        get => _paymentMethod;
        private set
        {
            if (SetProperty(ref _paymentMethod, value))
            {
                OnPropertyChanged(nameof(PaymentMethodTitle));
                OnPropertyChanged(nameof(IsCashPayment));
                OnPropertyChanged(nameof(IsCardPayment));
                OnPropertyChanged(nameof(ChangeAmount));
                OnPropertyChanged(nameof(ChangeAmountText));
                RaiseCommandStates();
            }
        }
    }

    public string ReceivedAmountText
    {
        get => _receivedAmountText;
        set
        {
            if (SetProperty(ref _receivedAmountText, value))
            {
                UpdateReceiptPaymentsPreview();
                OnPropertyChanged(nameof(ReceivedAmount));
                OnPropertyChanged(nameof(ChangeAmount));
                OnPropertyChanged(nameof(ChangeAmountText));
                RaiseCommandStates();
            }
        }
    }

    public decimal ReceivedAmount => ParseAmount(ReceivedAmountText);

    public string PaymentMethodTitle => PaymentMethods.FirstOrDefault(x => x.Key == PaymentMethod)?.Title ?? PaymentMethod;

    public decimal ChangeAmount => CurrentReceipt.ChangeAmount;

    public string ChangeAmountText => ChangeAmount.ToString("0.00", CultureInfo.InvariantCulture);

    public bool IsCashPayment => PaymentMethod == "Cash";

    public bool IsCardPayment => PaymentMethod == "Card";

    public PosState CurrentState => _stateManager.CurrentState;

    public bool IsIdle => _stateManager.IsInState(PosState.Idle);

    public bool IsScanning => _stateManager.IsInState(PosState.Scanning);

    public bool IsBasketEditing => _stateManager.IsInState(PosState.BasketEditing);

    public bool IsPayment => _stateManager.IsInState(PosState.Payment);

    public string HeldSalesCountText => $"{HeldSales.Count} askıdaki fiş";

    public string CompletedReceiptsCountText => $"{CompletedReceipts.Count} tamamlanan fiş";

    public void RegisterBarcodeCharacter()
    {
        var now = DateTime.UtcNow;

        if (_lastBarcodeCharacterAtUtc.HasValue)
        {
            var elapsed = now - _lastBarcodeCharacterAtUtc.Value;
            IsScannerInputActive = elapsed.TotalMilliseconds <= ScannerThresholdMilliseconds;
        }
        else
        {
            IsScannerInputActive = false;
        }

        _lastBarcodeCharacterAtUtc = now;

        if (IsScannerInputActive)
        {
            RestartScannerCompleteTimer();
        }
        else
        {
            _scannerCompleteTimer.Stop();
        }
    }

    public void FinalizeBarcodeEntry()
    {
        _scannerCompleteTimer.Stop();
        _lastBarcodeCharacterAtUtc = null;
    }

    public void AddProduct(string productName)
    {
        if (!_productPrices.TryGetValue(productName, out var unitPrice))
        {
            return;
        }

        var existingItem = BasketItems.FirstOrDefault(x => x.ProductName == productName);

        foreach (var item in BasketItems)
        {
            item.IsLastAdded = false;
        }

        if (existingItem is null)
        {
            existingItem = new BasketItem
            {
                ProductName = productName,
                Quantity = 1,
                UnitPrice = unitPrice,
                IsLastAdded = true
            };

            BasketItems.Add(existingItem);
        }
        else
        {
            existingItem.Quantity += 1;
            existingItem.IsLastAdded = true;
        }

        SelectedItem = existingItem;
        LastScannedProduct = $"Son ürün: {productName}";
        StartBasketEditing();
        RefreshDisplayValues();
    }

    public void SetIdleState()
    {
        ChangeState(PosState.Idle);
    }

    public void StartScanning()
    {
        ChangeState(PosState.Scanning);
    }

    public void StartBasketEditing()
    {
        ChangeState(PosState.BasketEditing);
    }

    public void StartPayment()
    {
        ChangeState(PosState.Payment);
    }

    private Receipt CreateReceipt()
    {
        return new Receipt();
    }

    private void SubscribeToReceipt(Receipt receipt)
    {
        receipt.PropertyChanged += OnCurrentReceiptPropertyChanged;
        receipt.Items.CollectionChanged += OnCurrentReceiptItemsChanged;
    }

    private void UnsubscribeFromReceipt(Receipt receipt)
    {
        receipt.PropertyChanged -= OnCurrentReceiptPropertyChanged;
        receipt.Items.CollectionChanged -= OnCurrentReceiptItemsChanged;
    }

    private void OnCurrentReceiptPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Receipt.SubtotalAmount) or nameof(Receipt.TotalAmount) or nameof(Receipt.DiscountAmount) or nameof(Receipt.RoundAdjustment) or nameof(Receipt.CashAmount) or nameof(Receipt.CardAmount) or nameof(Receipt.ChangeAmount) or nameof(Receipt.IsPaid))
        {
            OnPropertyChanged(nameof(ReceiptDiscountSubtotal));
            OnPropertyChanged(nameof(ReceiptDiscountValue));
            OnPropertyChanged(nameof(ReceiptDiscountRoundAdjustment));
            OnPropertyChanged(nameof(ReceiptDiscountPreviewTotal));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(ChangeAmountText));
            RaiseCommandStates();
        }
    }

    private void OnCurrentReceiptItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(BasketItems));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(ReceiptDiscountSubtotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(ChangeAmountText));
        RaiseCommandStates();
    }

    private void AddProductByBarcode(string barcode)
    {
        StartScanning();

        var normalizedBarcode = barcode.Trim();

        if (_barcodeProductMap.TryGetValue(normalizedBarcode, out var productName))
        {
            AddProduct(productName);
            AddBarcodeHistory(normalizedBarcode, productName);
            LastInvalidBarcode = string.Empty;
        }
        else
        {
            LastScannedProduct = $"Barkod bulunamadı: {normalizedBarcode}";
            LastInvalidBarcode = $"Geçersiz barkod: {normalizedBarcode}";
            AddBarcodeHistory(normalizedBarcode, "Bilinmiyor");
            AppDialogService.ShowOperationFailed(
                "Barkod Bulunamadı",
                $"{normalizedBarcode} barkodu sistemde kayıtlı değil.",
                "POS-BARKOD-404");
        }

        Barcode = string.Empty;
        IsScannerInputActive = false;
        FinalizeBarcodeEntry();
    }

    private void RestartScannerCompleteTimer()
    {
        _scannerCompleteTimer.Stop();
        _scannerCompleteTimer.Start();
    }

    private void ScannerCompleteTimer_Tick(object? sender, EventArgs e)
    {
        _scannerCompleteTimer.Stop();

        if (!IsScannerInputActive || string.IsNullOrWhiteSpace(Barcode))
        {
            FinalizeBarcodeEntry();
            return;
        }

        if (Barcode.Trim().Length < MinimumScannerBarcodeLength)
        {
            IsScannerInputActive = false;
            FinalizeBarcodeEntry();
            return;
        }

        AddProductByBarcode(Barcode);
    }

    private void AddBarcodeHistory(string barcode, string productName)
    {
        BarcodeHistory.Insert(0, new BarcodeHistoryItem(barcode, productName, DateTime.Now));

        while (BarcodeHistory.Count > MaxHistoryItems)
        {
            BarcodeHistory.RemoveAt(BarcodeHistory.Count - 1);
        }
    }

    private void SelectQuickProductCategory(QuickProductCategoryItem category)
    {
        if (SelectedQuickProductCategory is not null)
        {
            SelectedQuickProductCategory.IsSelected = false;
        }

        SelectedQuickProductCategory = category;
        SelectedQuickProductCategory.IsSelected = true;
        ApplyQuickProductCategory(category.Key);
    }

    private void SelectActionTab(PosActionTabItem tab)
    {
        if (SelectedActionTab is not null)
        {
            SelectedActionTab.IsSelected = false;
        }

        SelectedActionTab = tab;
        SelectedActionTab.IsSelected = true;
        ApplyActionTab(tab.Key);
    }

    private void ApplyQuickProductCategory(string categoryKey)
    {
        QuickProducts.Clear();

        var filteredProducts = categoryKey == AllQuickProductCategoryKey
            ? _allQuickProducts
            : _allQuickProducts.Where(x => x.CategoryKey == categoryKey);

        foreach (var product in filteredProducts)
        {
            QuickProducts.Add(product);
        }
    }

    private void ApplyActionTab(string tabKey)
    {
        VisibleActionItems.Clear();

        if (tabKey == QuickProductsTabKey)
        {
            RefreshActionItemStates();
            return;
        }

        foreach (var actionItem in _allActionItems.Where(x => x.TabKey == tabKey))
        {
            VisibleActionItems.Add(actionItem);
        }

        RefreshActionItemStates();
    }

    private void DecreaseItemQuantity(BasketItem item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity -= 1;
        }
        else
        {
            BasketItems.Remove(item);

            if (ReferenceEquals(SelectedItem, item))
            {
                SelectedItem = null;
            }
        }

        RefreshDisplayValues();
    }

    private void IncreaseItemQuantity(BasketItem item)
    {
        item.Quantity += 1;
        RefreshDisplayValues();
    }

    private void RemoveItem(BasketItem item)
    {
        if (!AppDialogService.ShowDeleteConfirmation(item.ProductName, "Seçili ürün sepetten kaldırılacak."))
        {
            return;
        }

        BasketItems.Remove(item);

        if (ReferenceEquals(SelectedItem, item))
        {
            SelectedItem = null;
        }

        RefreshDisplayValues();
    }

    private void TriggerPosAction(PosActionItem actionItem)
    {
        if (!actionItem.IsEnabled)
        {
            var message = actionItem.Scope == PosActionScope.Product
                ? "Bu işlem için önce sepetten bir ürün seçin."
                : "Bu işlem için sepette en az bir ürün olmalı.";
            AppDialogService.ShowWarning("İşlem Kullanılamıyor", message);
            return;
        }

        switch (actionItem.Key)
        {
            case "receipt-discount":
                OpenReceiptDiscountScreen();
                return;
            case "receipt-suspend":
                SuspendCurrentSale();
                return;
            case "receipt-held-list":
                OpenHeldSalesScreen();
                return;
            case "receipt-history":
                OpenReceiptHistoryScreen();
                return;
            case "line-remove":
                if (SelectedItem is not null)
                {
                    RemoveItem(SelectedItem);
                }
                return;
            default:
                AppDialogService.ShowInfo(actionItem.Title, $"{actionItem.Title} ekranı bir sonraki adımda detaylı olarak bağlanacak.");
                return;
        }
    }

    private void OpenReceiptDiscountScreen()
    {
        if (BasketItems.Count == 0)
        {
            return;
        }

        IsPaymentScreenOpen = false;
        IsHeldSalesScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        LoadCurrentReceiptDiscountState();
        IsReceiptDiscountScreenOpen = true;
    }

    private void CloseReceiptDiscountScreen()
    {
        IsReceiptDiscountScreenOpen = false;
    }

    private void LoadCurrentReceiptDiscountState()
    {
        _isUpdatingReceiptDiscountInputs = true;
        ReceiptDiscountRateText = string.Empty;
        ReceiptDiscountAmountText = CurrentReceipt.DiscountAmount > 0m
            ? CurrentReceipt.DiscountAmount.ToString("0.00", CultureInfo.InvariantCulture)
            : string.Empty;
        _isUpdatingReceiptDiscountInputs = false;
        ReceiptDiscountLabel = string.IsNullOrWhiteSpace(CurrentReceipt.DiscountLabel) ? "Manuel düzenleme" : CurrentReceipt.DiscountLabel;
        ReceiptRoundMode = CurrentReceipt.RoundAdjustment switch
        {
            > 0m => ReceiptRoundMode.Up,
            < 0m => ReceiptRoundMode.Down,
            _ => ReceiptRoundMode.None
        };
        ReceiptDiscountInputTarget = DiscountInputAmountTarget;
        NotifyReceiptDiscountPreviewChanged();
    }

    private void ApplyReceiptDiscount()
    {
        var discountAmount = CalculateReceiptDiscountValue();
        var roundAdjustment = CalculateReceiptRoundAdjustment();
        var label = string.IsNullOrWhiteSpace(ReceiptDiscountLabel) ? "Fiş indirimi" : ReceiptDiscountLabel;

        CurrentReceipt.ApplyReceiptAdjustment(discountAmount, roundAdjustment, label);
        IsReceiptDiscountScreenOpen = false;
        RefreshDisplayValues();
        AppDialogService.ShowToastSuccess("Fiş İndirimi Uygulandı", $"{label} fişe uygulandı.", 3);
    }

    private void SelectReceiptDiscountPreset(ReceiptDiscountPresetItem preset)
    {
        _isUpdatingReceiptDiscountInputs = true;
        if (preset.Kind == ReceiptDiscountPresetKind.Percentage)
        {
            ReceiptDiscountRateText = preset.Value.ToString("0.##", CultureInfo.InvariantCulture);
            ReceiptDiscountAmountText = string.Empty;
        }
        else
        {
            ReceiptDiscountRateText = string.Empty;
            ReceiptDiscountAmountText = preset.Value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        _isUpdatingReceiptDiscountInputs = false;
        ReceiptDiscountLabel = preset.Title;
        NotifyReceiptDiscountPreviewChanged();
    }

    private void AppendReceiptDiscountInput(string value)
    {
        var currentValue = ReceiptDiscountInputTarget == DiscountInputRateTarget
            ? ReceiptDiscountRateText
            : ReceiptDiscountAmountText;

        if (value == "," || value == ".")
        {
            if (currentValue.Contains(',') || currentValue.Contains('.'))
            {
                return;
            }

            currentValue += CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        }
        else
        {
            currentValue += value;
        }

        SetReceiptDiscountInputValue(currentValue);
    }

    private void BackspaceReceiptDiscountInput()
    {
        var currentValue = ReceiptDiscountInputTarget == DiscountInputRateTarget
            ? ReceiptDiscountRateText
            : ReceiptDiscountAmountText;

        if (string.IsNullOrEmpty(currentValue))
        {
            return;
        }

        SetReceiptDiscountInputValue(currentValue[..^1]);
    }

    private void ClearReceiptDiscountInput()
    {
        SetReceiptDiscountInputValue(string.Empty);
    }

    private void SetReceiptDiscountInputValue(string value)
    {
        if (ReceiptDiscountInputTarget == DiscountInputRateTarget)
        {
            ReceiptDiscountRateText = value;
            return;
        }

        ReceiptDiscountAmountText = value;
    }

    private void SetReceiptRoundMode(string roundMode)
    {
        ReceiptRoundMode = roundMode switch
        {
            "Up" => ReceiptRoundMode.Up,
            "Down" => ReceiptRoundMode.Down,
            _ => ReceiptRoundMode.None
        };
    }

    private decimal CalculateReceiptDiscountValue()
    {
        var subtotal = ReceiptDiscountSubtotal;
        var rate = ParseAmount(ReceiptDiscountRateText);
        var amount = ParseAmount(ReceiptDiscountAmountText);

        if (rate > 0m)
        {
            return Math.Min(subtotal, decimal.Round(subtotal * rate / 100m, 2));
        }

        return Math.Min(subtotal, decimal.Round(amount, 2));
    }

    private decimal CalculateReceiptRoundAdjustment()
    {
        var baseTotal = Math.Max(0m, ReceiptDiscountSubtotal - CalculateReceiptDiscountValue());

        return ReceiptRoundMode switch
        {
            ReceiptRoundMode.Up => decimal.Round(Math.Ceiling(baseTotal) - baseTotal, 2),
            ReceiptRoundMode.Down => decimal.Round(Math.Floor(baseTotal) - baseTotal, 2),
            _ => 0m
        };
    }

    private void NotifyReceiptDiscountPreviewChanged()
    {
        OnPropertyChanged(nameof(ReceiptDiscountSubtotal));
        OnPropertyChanged(nameof(ReceiptDiscountValue));
        OnPropertyChanged(nameof(ReceiptDiscountRoundAdjustment));
        OnPropertyChanged(nameof(ReceiptDiscountPreviewTotal));
    }

    private void OpenPaymentScreen(string paymentType)
    {
        IsHeldSalesScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        IsPaymentScreenOpen = true;
        StartPayment();
        SelectPaymentMethod(paymentType);
        RaiseCommandStates();
    }

    private void OpenDefaultPaymentScreen()
    {
        var defaultMethod = _settingsService.DefaultPaymentMethod;

        if (!PaymentMethods.Any(x => x.Key == defaultMethod))
        {
            defaultMethod = PaymentMethods.FirstOrDefault()?.Key ?? PaymentMethod;
        }

        OpenPaymentScreen(defaultMethod);
    }

    private void SelectPaymentMethod(string paymentType)
    {
        PaymentMethod = paymentType;

        if (paymentType == "Cash")
        {
            if (string.IsNullOrWhiteSpace(ReceivedAmountText))
            {
                ReceivedAmountText = CurrentReceipt.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }
        else
        {
            ReceivedAmountText = string.Empty;
        }

        UpdateReceiptPaymentsPreview();
    }

    private void SetQuickReceivedAmount(string amountKey)
    {
        if (!IsCashPayment)
        {
            return;
        }

        ReceivedAmountText = amountKey switch
        {
            "TOTAL" => CurrentReceipt.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
            _ => amountKey
        };
    }

    private void UpdateReceiptPaymentsPreview()
    {
        if (!IsPaymentScreenOpen)
        {
            return;
        }

        if (IsCashPayment)
        {
            CurrentReceipt.CashAmount = ReceivedAmount;
            CurrentReceipt.CardAmount = 0m;
        }
        else
        {
            CurrentReceipt.CashAmount = 0m;
            CurrentReceipt.CardAmount = CurrentReceipt.TotalAmount;
        }
    }

    private void CancelPaymentScreen()
    {
        if (CurrentReceipt.PaidAmount > 0 || !string.IsNullOrWhiteSpace(ReceivedAmountText))
        {
            AppDialogService.ShowWarning("Ödeme İptal Edildi", "Ödeme ekranı kapatıldı. Satış sepeti korunuyor.");
        }

        IsPaymentScreenOpen = false;
        ReceivedAmountText = string.Empty;
        CurrentReceipt.ResetPayments();

        if (BasketItems.Count == 0)
        {
            SetIdleState();
        }
        else
        {
            StartBasketEditing();
        }

        RaiseCommandStates();
    }

    private void SuspendCurrentSale()
    {
        if (BasketItems.Count == 0)
        {
            return;
        }

        var heldSaleNumber = HeldSales.Count + 1;
        var heldSale = new HeldSaleItem(
            $"ASKI-{heldSaleNumber:000}",
            CloneBasketItems(BasketItems),
            CurrentReceipt.TotalAmount,
            DateTime.Now);

        HeldSales.Insert(0, heldSale);

        CurrentReceipt = CreateReceipt();
        SelectedItem = null;
        Barcode = string.Empty;
        ReceivedAmountText = string.Empty;
        LastScannedProduct = $"{heldSale.ReferenceNo} askıya alındı";
        LastInvalidBarcode = string.Empty;
        IsPaymentScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        FinalizeBarcodeEntry();
        SetIdleState();
        RefreshDisplayValues();
        RaiseCommandStates();
    }

    private void OpenHeldSalesScreen()
    {
        if (HeldSales.Count == 0)
        {
            AppDialogService.ShowWarning("Askı Listesi Boş", "Geri çağrılabilecek askı fişi bulunmuyor.");
            return;
        }

        IsPaymentScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        IsHeldSalesScreenOpen = true;
    }

    private void CloseHeldSalesScreen()
    {
        IsHeldSalesScreenOpen = false;
    }

    private void OpenReceiptHistoryScreen()
    {
        if (CompletedReceipts.Count == 0)
        {
            AppDialogService.ShowWarning("Fiş Geçmişi Boş", "Henüz tamamlanmış satış bulunmuyor.");
            return;
        }

        IsPaymentScreenOpen = false;
        IsHeldSalesScreenOpen = false;
        IsReceiptHistoryScreenOpen = true;
    }

    private void CloseReceiptHistoryScreen()
    {
        IsReceiptHistoryScreenOpen = false;
    }

    private void PrintReceipt(Receipt receipt)
    {
        if (string.IsNullOrWhiteSpace(_settingsService.ReceiptPrinterName))
        {
            AppDialogService.ShowOperationFailed(
                "Yazıcı Bulunamadı",
                "Fiş yazıcı adı tanımlı değil. Ayarlardan bir fiş yazıcısı seçin.",
                "POS-PRN-001");
            return;
        }

        AppDialogService.ShowInfo(
            "Fiş Yazdır",
            $"Fiş: {receipt.ReceiptNo}\nTarih: {receipt.Date:dd.MM.yyyy HH:mm:ss}\nToplam: {receipt.TotalAmount:0.00}\nÖdenen: {receipt.PaidAmount:0.00}\nYazıcı: {_settingsService.ReceiptPrinterName}");
    }

    private void RestoreCompletedReceipt(Receipt receipt)
    {
        var restoredReceipt = CreateReceipt();

        foreach (var item in receipt.Items)
        {
            restoredReceipt.Items.Add(new BasketItem
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                IsLastAdded = false
            });
        }

        CurrentReceipt = restoredReceipt;
        SelectedItem = BasketItems.FirstOrDefault();
        Barcode = string.Empty;
        ReceivedAmountText = string.Empty;
        LastScannedProduct = $"{receipt.ReceiptNo} tekrar satışa aktarıldı";
        LastInvalidBarcode = string.Empty;
        IsPaymentScreenOpen = false;
        IsHeldSalesScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        FinalizeBarcodeEntry();
        StartBasketEditing();
        RefreshDisplayValues();
        RaiseCommandStates();
    }

    private void RecallHeldSale(HeldSaleItem heldSale)
    {
        var recalledReceipt = CreateReceipt();

        foreach (var item in heldSale.Items)
        {
            recalledReceipt.Items.Add(new BasketItem
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                IsLastAdded = false
            });
        }

        HeldSales.Remove(heldSale);
        CurrentReceipt = recalledReceipt;
        IsHeldSalesScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        SelectedItem = BasketItems.FirstOrDefault();
        Barcode = string.Empty;
        ReceivedAmountText = string.Empty;
        LastScannedProduct = $"{heldSale.ReferenceNo} geri çağrıldı";
        LastInvalidBarcode = string.Empty;
        StartBasketEditing();
        RefreshDisplayValues();
        RaiseCommandStates();
    }

    private bool CanConfirmPayment()
    {
        return BasketItems.Count > 0 &&
               IsPaymentScreenOpen &&
               CurrentReceipt.IsPaid;
    }

    private void ConfirmPayment()
    {
        if (!CurrentReceipt.IsPaid)
        {
            var remainingAmount = CurrentReceipt.TotalAmount - CurrentReceipt.PaidAmount;
            AppDialogService.ShowOperationFailed(
                "Yetersiz Ödeme",
                $"Ödeme tamamlanamadı. Kalan tutar: {Math.Max(remainingAmount, 0m):0.00}",
                "POS-PAY-402");
            return;
        }

        var completedReceipt = CurrentReceipt;
        var paymentType = PaymentMethod;

        CompletedReceipts.Insert(0, completedReceipt);

        CurrentReceipt = CreateReceipt();
        SelectedItem = null;
        Barcode = string.Empty;
        ReceivedAmountText = string.Empty;
        LastScannedProduct = $"{completedReceipt.ReceiptNo} tamamlandı";
        LastInvalidBarcode = string.Empty;
        IsScannerInputActive = false;
        IsPaymentScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        FinalizeBarcodeEntry();
        SetIdleState();
        RefreshDisplayValues();
        RaiseCommandStates();

        var paymentTitle = PaymentMethods.FirstOrDefault(x => x.Key == paymentType)?.Title ?? paymentType;

        AppDialogService.ShowSuccess(
            "Ödeme Başarılı",
            paymentType == "Cash"
                ? $"{paymentTitle} ödemesi alındı.\nFiş: {completedReceipt.ReceiptNo}\nToplam: {completedReceipt.TotalAmount:0.00}\nPara Üstü: {completedReceipt.ChangeAmount:0.00}"
                : $"{paymentTitle} ödemesi alındı.\nFiş: {completedReceipt.ReceiptNo}\nToplam: {completedReceipt.TotalAmount:0.00}");
    }

    private void RefreshDisplayValues()
    {
        OnPropertyChanged(nameof(BasketItems));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(ReceiptDiscountSubtotal));
        OnPropertyChanged(nameof(ReceiptDiscountValue));
        OnPropertyChanged(nameof(ReceiptDiscountRoundAdjustment));
        OnPropertyChanged(nameof(ReceiptDiscountPreviewTotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(ChangeAmountText));
        OnPropertyChanged(nameof(ReceiptNo));
        OnPropertyChanged(nameof(HeldSalesCountText));
        OnPropertyChanged(nameof(CompletedReceiptsCountText));
        RefreshActionItemStates();
    }

    private void RefreshActionItemStates()
    {
        var hasSelectedItem = SelectedItem is not null;
        var hasBasket = BasketItems.Count > 0;
        var hasHeldSales = HeldSales.Count > 0;
        var hasCompletedReceipts = CompletedReceipts.Count > 0;

        foreach (var actionItem in _allActionItems)
        {
            actionItem.IsEnabled = actionItem.Key switch
            {
                "receipt-held-list" => hasHeldSales,
                "receipt-history" => hasCompletedReceipts,
                _ when actionItem.Scope == PosActionScope.Product => hasSelectedItem,
                _ => hasBasket
            };
        }
    }

    private void ChangeState(PosState state)
    {
        _stateManager.SetState(state);

        OnPropertyChanged(nameof(CurrentState));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsScanning));
        OnPropertyChanged(nameof(IsBasketEditing));
        OnPropertyChanged(nameof(IsPayment));
    }

    private void RaiseCommandStates()
    {
        ((RelayCommand)CashPaymentCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CardPaymentCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ReceivePaymentCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenPaymentMethodCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CompletePaymentCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SuspendSaleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenHeldSalesCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenReceiptHistoryCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenReceiptDiscountCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ApplyReceiptDiscountCommand).RaiseCanExecuteChanged();
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureAmount))
        {
            return currentCultureAmount;
        }

        if (decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantAmount))
        {
            return invariantAmount;
        }

        return 0m;
    }

    private static List<BasketItemSnapshot> CloneBasketItems(IEnumerable<BasketItem> items)
    {
        return items.Select(item => new BasketItemSnapshot(item.ProductName, item.Quantity, item.UnitPrice)).ToList();
    }
}

public sealed class QuickProductItem
{
    public QuickProductItem(string name, string categoryKey)
    {
        Name = name;
        CategoryKey = categoryKey;
    }

    public string Name { get; }

    public string CategoryKey { get; }
}

public sealed class QuickProductCategoryItem : ViewModelBase
{
    private bool _isSelected;

    public QuickProductCategoryItem(string key, string title)
    {
        Key = key;
        Title = title;
    }

    public string Key { get; }

    public string Title { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class PosActionTabItem : ViewModelBase
{
    private bool _isSelected;

    public PosActionTabItem(string key, string title)
    {
        Key = key;
        Title = title;
    }

    public string Key { get; }

    public string Title { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class PosActionItem : ViewModelBase
{
    private bool _isEnabled;

    public PosActionItem(string key, string tabKey, string title, string subtitle)
    {
        Key = key;
        TabKey = tabKey;
        Title = title;
        Subtitle = subtitle;
        Scope = tabKey == "product" ? PosActionScope.Product : PosActionScope.Receipt;
    }

    public string Key { get; }

    public string TabKey { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public PosActionScope Scope { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}

public enum PosActionScope
{
    Product,
    Receipt
}

public sealed class ReceiptDiscountPresetItem
{
    public ReceiptDiscountPresetItem(string title, ReceiptDiscountPresetKind kind, decimal value)
    {
        Title = title;
        Kind = kind;
        Value = value;
    }

    public string Title { get; }

    public ReceiptDiscountPresetKind Kind { get; }

    public decimal Value { get; }

    public string ValueText => Kind == ReceiptDiscountPresetKind.Percentage
        ? $"%{Value:0.##}"
        : $"{Value:0.00} TL";
}

public enum ReceiptDiscountPresetKind
{
    Percentage,
    Amount
}

public enum ReceiptRoundMode
{
    None,
    Up,
    Down
}

public sealed class BarcodeHistoryItem
{
    public BarcodeHistoryItem(string barcode, string productName, DateTime scannedAt)
    {
        Barcode = barcode;
        ProductName = productName;
        ScannedAt = scannedAt;
    }

    public string Barcode { get; }

    public string ProductName { get; }

    public DateTime ScannedAt { get; }

    public string TimeText => ScannedAt.ToString("HH:mm:ss");
}

public sealed class HeldSaleItem
{
    public HeldSaleItem(string referenceNo, List<BasketItemSnapshot> items, decimal totalAmount, DateTime createdAt)
    {
        ReferenceNo = referenceNo;
        Items = items;
        TotalAmount = totalAmount;
        CreatedAt = createdAt;
    }

    public string ReferenceNo { get; }

    public List<BasketItemSnapshot> Items { get; }

    public decimal TotalAmount { get; }

    public DateTime CreatedAt { get; }

    public int ItemCount => Items.Sum(x => x.Quantity);

    public string TimeText => CreatedAt.ToString("HH:mm:ss");
}

public sealed class CompletedReceiptItem
{
    public CompletedReceiptItem(Receipt receipt)
    {
        Receipt = receipt;
    }

    public Receipt Receipt { get; }
}

public sealed class BasketItemSnapshot
{
    public BasketItemSnapshot(string productName, int quantity, decimal unitPrice)
    {
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string ProductName { get; }

    public int Quantity { get; }

    public decimal UnitPrice { get; }
}

