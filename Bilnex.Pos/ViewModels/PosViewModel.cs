using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
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

    private readonly Dictionary<string, decimal> _productPrices = new()
    {
        ["Water"] = 10m,
        ["Bread"] = 15m,
        ["Milk"] = 35m,
        ["Cola"] = 40m
    };

    private readonly Dictionary<string, string> _barcodeProductMap = new()
    {
        ["111"] = "Water",
        ["222"] = "Bread",
        ["333"] = "Milk",
        ["444"] = "Cola"
    };

    private readonly PosStateManager _stateManager;
    private readonly DispatcherTimer _scannerCompleteTimer;
    private readonly PosSettingsService _settingsService;

    private Receipt _currentReceipt;
    private BasketItem? _selectedItem;
    private string _barcode = string.Empty;
    private bool _isScannerInputActive;
    private string _lastScannedProduct = "No product scanned yet";
    private string _lastInvalidBarcode = string.Empty;
    private bool _isPaymentScreenOpen;
    private bool _isHeldSalesScreenOpen;
    private bool _isReceiptHistoryScreenOpen;
    private string _paymentMethod = "Cash";
    private string _receivedAmountText = string.Empty;
    private DateTime? _lastBarcodeCharacterAtUtc;

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

        QuickProducts = new ObservableCollection<QuickProductItem>
        {
            new("Water"),
            new("Bread"),
            new("Milk"),
            new("Cola")
        };
        BarcodeHistory = new ObservableCollection<BarcodeHistoryItem>();
        HeldSales = new ObservableCollection<HeldSaleItem>();
        CompletedReceipts = new ObservableCollection<Receipt>();

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
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(ChangeAmountText));
            RaiseCommandStates();
        }
    }

    public ObservableCollection<BasketItem> BasketItems => CurrentReceipt.Items;

    public ObservableCollection<QuickProductItem> QuickProducts { get; }

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

    public BasketItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public string ReceiptNo => CurrentReceipt.ReceiptNo;

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

    public decimal ChangeAmount => CurrentReceipt.ChangeAmount;

    public string ChangeAmountText => ChangeAmount.ToString("0.00", CultureInfo.InvariantCulture);

    public bool IsCashPayment => PaymentMethod == "Cash";

    public bool IsCardPayment => PaymentMethod == "Card";

    public PosState CurrentState => _stateManager.CurrentState;

    public bool IsIdle => _stateManager.IsInState(PosState.Idle);

    public bool IsScanning => _stateManager.IsInState(PosState.Scanning);

    public bool IsBasketEditing => _stateManager.IsInState(PosState.BasketEditing);

    public bool IsPayment => _stateManager.IsInState(PosState.Payment);

    public string HeldSalesCountText => $"{HeldSales.Count} askida fis";

    public string CompletedReceiptsCountText => $"{CompletedReceipts.Count} tamamlanan fis";

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
        LastScannedProduct = $"Last product: {productName}";
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
        if (e.PropertyName is nameof(Receipt.TotalAmount) or nameof(Receipt.CashAmount) or nameof(Receipt.CardAmount) or nameof(Receipt.ChangeAmount) or nameof(Receipt.IsPaid))
        {
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
            LastScannedProduct = $"Barcode not found: {normalizedBarcode}";
            LastInvalidBarcode = $"Invalid barcode: {normalizedBarcode}";
            AddBarcodeHistory(normalizedBarcode, "Unknown");
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
        BasketItems.Remove(item);

        if (ReferenceEquals(SelectedItem, item))
        {
            SelectedItem = null;
        }

        RefreshDisplayValues();
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
        LastScannedProduct = $"{heldSale.ReferenceNo} askiya alindi";
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
        MessageBox.Show(
            $"Receipt: {receipt.ReceiptNo}\nDate: {receipt.Date:dd.MM.yyyy HH:mm:ss}\nTotal: {receipt.TotalAmount:0.00}\nPaid: {receipt.PaidAmount:0.00}\nPrinter: {_settingsService.ReceiptPrinterName}",
            "Receipt Print",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
        LastScannedProduct = $"{receipt.ReceiptNo} tekrar satisa aktarildi";
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
        LastScannedProduct = $"{heldSale.ReferenceNo} geri cagrildi";
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
            return;
        }

        var completedReceipt = CurrentReceipt;
        var paymentType = PaymentMethod;

        CompletedReceipts.Insert(0, completedReceipt);

        CurrentReceipt = CreateReceipt();
        SelectedItem = null;
        Barcode = string.Empty;
        ReceivedAmountText = string.Empty;
        LastScannedProduct = $"{completedReceipt.ReceiptNo} completed";
        LastInvalidBarcode = string.Empty;
        IsScannerInputActive = false;
        IsPaymentScreenOpen = false;
        IsReceiptHistoryScreenOpen = false;
        FinalizeBarcodeEntry();
        SetIdleState();
        RefreshDisplayValues();
        RaiseCommandStates();

        MessageBox.Show(
            paymentType == "Cash"
                ? $"{paymentType} payment received.\nReceipt: {completedReceipt.ReceiptNo}\nTotal: {completedReceipt.TotalAmount:0.00}\nChange: {completedReceipt.ChangeAmount:0.00}"
                : $"{paymentType} payment received.\nReceipt: {completedReceipt.ReceiptNo}\nTotal: {completedReceipt.TotalAmount:0.00}",
            "Payment Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void RefreshDisplayValues()
    {
        OnPropertyChanged(nameof(BasketItems));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(ChangeAmountText));
        OnPropertyChanged(nameof(ReceiptNo));
        OnPropertyChanged(nameof(HeldSalesCountText));
        OnPropertyChanged(nameof(CompletedReceiptsCountText));
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
    public QuickProductItem(string name)
    {
        Name = name;
    }

    public string Name { get; }
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
