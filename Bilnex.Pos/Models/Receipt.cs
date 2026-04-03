using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bilnex.Pos.Models;

public sealed class Receipt : INotifyPropertyChanged
{
    private decimal _cashAmount;
    private decimal _cardAmount;
    private decimal _discountAmount;
    private decimal _discountRate;
    private decimal _roundAdjustment;
    private string _discountLabel = string.Empty;

    public Receipt()
    {
        ReceiptNo = GenerateReceiptNo();
        Date = DateTime.Now;
        Items = new ObservableCollection<BasketItem>();
        Items.CollectionChanged += OnItemsCollectionChanged;
        PaymentEntries = new ObservableCollection<PaymentEntry>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ReceiptNo { get; }

    public DateTime Date { get; }

    public ObservableCollection<BasketItem> Items { get; }

    public ObservableCollection<PaymentEntry> PaymentEntries { get; }

    public bool HasPaymentEntries => PaymentEntries.Count > 0;

    public int ItemCount => Items.Sum(x => x.Quantity);

    public decimal SubtotalAmount => Items.Sum(x => x.LineTotal);

    /// <summary>Sum of OriginalUnitPrice × Quantity for every item (before any line discounts).</summary>
    public decimal OriginalSubtotalAmount => Items.Sum(x =>
        x.OriginalUnitPrice > 0m ? x.OriginalUnitPrice * x.Quantity : x.LineTotal);

    /// <summary>Total savings coming from per-line discounts (OriginalSubtotal – Subtotal).</summary>
    public decimal LineDiscountAmount => Math.Max(0m, OriginalSubtotalAmount - SubtotalAmount);

    public decimal DiscountAmount
    {
        get => _discountAmount;
        private set
        {
            if (SetField(ref _discountAmount, value))
            {
                NotifyAmountPropertiesChanged();
            }
        }
    }

    public decimal DiscountRate
    {
        get => _discountRate;
        private set => SetField(ref _discountRate, value);
    }

    public decimal RoundAdjustment
    {
        get => _roundAdjustment;
        private set
        {
            if (SetField(ref _roundAdjustment, value))
            {
                NotifyAmountPropertiesChanged();
            }
        }
    }

    public string DiscountLabel
    {
        get => _discountLabel;
        private set => SetField(ref _discountLabel, value);
    }

    public decimal TotalAmount => Math.Max(0m, SubtotalAmount - DiscountAmount + RoundAdjustment);

    public decimal CashAmount
    {
        get => _cashAmount;
        set
        {
            if (SetField(ref _cashAmount, value))
            {
                NotifyPaymentPropertiesChanged();
            }
        }
    }

    public decimal CardAmount
    {
        get => _cardAmount;
        set
        {
            if (SetField(ref _cardAmount, value))
            {
                NotifyPaymentPropertiesChanged();
            }
        }
    }

    public decimal PaidAmount => CashAmount + CardAmount;

    public decimal ChangeAmount => PaidAmount - TotalAmount;

    public bool IsPaid => PaidAmount >= TotalAmount && TotalAmount > 0;

    public void ResetPayments()
    {
        CashAmount = 0m;
        CardAmount = 0m;
    }

    public void ApplyReceiptAdjustment(decimal discountAmount, decimal roundAdjustment, string? discountLabel, decimal discountRate = 0m)
    {
        var subtotal = SubtotalAmount;
        var clampedDiscount = Math.Max(0m, Math.Min(subtotal, decimal.Round(discountAmount, 2)));

        DiscountAmount = clampedDiscount;
        DiscountRate = Math.Max(0m, decimal.Round(discountRate, 4));
        RoundAdjustment = decimal.Round(roundAdjustment, 2);
        DiscountLabel = discountLabel ?? string.Empty;
    }

    public void ClearReceiptAdjustment()
    {
        DiscountAmount = 0m;
        DiscountRate = 0m;
        RoundAdjustment = 0m;
        DiscountLabel = string.Empty;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (BasketItem item in e.OldItems)
            {
                item.PropertyChanged -= OnBasketItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (BasketItem item in e.NewItems)
            {
                item.PropertyChanged += OnBasketItemPropertyChanged;
            }
        }

        NotifyAmountPropertiesChanged();
    }

    private void OnBasketItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BasketItem.Quantity) or nameof(BasketItem.UnitPrice) or nameof(BasketItem.LineTotal) or nameof(BasketItem.OriginalUnitPrice))
        {
            NotifyAmountPropertiesChanged();
        }
    }

    private void NotifyAmountPropertiesChanged()
    {
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(SubtotalAmount));
        OnPropertyChanged(nameof(OriginalSubtotalAmount));
        OnPropertyChanged(nameof(LineDiscountAmount));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(RoundAdjustment));
        OnPropertyChanged(nameof(DiscountLabel));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(IsPaid));
    }

    private void NotifyPaymentPropertiesChanged()
    {
        OnPropertyChanged(nameof(PaidAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(IsPaid));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string GenerateReceiptNo()
    {
        return $"R{DateTime.Now:ddHHmmss}";
    }
}
