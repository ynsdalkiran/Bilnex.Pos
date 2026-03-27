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

    public Receipt()
    {
        ReceiptNo = GenerateReceiptNo();
        Date = DateTime.Now;
        Items = new ObservableCollection<BasketItem>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ReceiptNo { get; }

    public DateTime Date { get; }

    public ObservableCollection<BasketItem> Items { get; }

    public int ItemCount => Items.Sum(x => x.Quantity);

    public decimal TotalAmount => Items.Sum(x => x.LineTotal);

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
        if (e.PropertyName is nameof(BasketItem.Quantity) or nameof(BasketItem.UnitPrice) or nameof(BasketItem.LineTotal))
        {
            NotifyAmountPropertiesChanged();
        }
    }

    private void NotifyAmountPropertiesChanged()
    {
        OnPropertyChanged(nameof(ItemCount));
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
