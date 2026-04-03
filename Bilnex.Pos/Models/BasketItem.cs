using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bilnex.Pos.Models;

public sealed class BasketItem : INotifyPropertyChanged
{
    private string _productName = string.Empty;
    private int _quantity;
    private decimal _unitPrice;
    private decimal _originalUnitPrice;
    private bool _isLastAdded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProductName
    {
        get => _productName;
        set => SetField(ref _productName, value);
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetField(ref _quantity, value))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            // Auto-initialise OriginalUnitPrice on first non-zero assignment.
            if (_originalUnitPrice == 0m && value > 0m)
            {
                _originalUnitPrice = value;
                OnPropertyChanged(nameof(OriginalUnitPrice));
                OnPropertyChanged(nameof(HasLineDiscount));
            }

            if (SetField(ref _unitPrice, value))
            {
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(HasLineDiscount));
            }
        }
    }

    /// <summary>The price when the item was first added or when a deliberate price change was applied.</summary>
    public decimal OriginalUnitPrice
    {
        get => _originalUnitPrice;
        set
        {
            if (SetField(ref _originalUnitPrice, value))
            {
                OnPropertyChanged(nameof(HasLineDiscount));
            }
        }
    }

    /// <summary>True when a discount has been applied and the current price is below the original.</summary>
    public bool HasLineDiscount => _originalUnitPrice > 0m && _unitPrice < _originalUnitPrice;

    public bool IsLastAdded
    {
        get => _isLastAdded;
        set => SetField(ref _isLastAdded, value);
    }

    public decimal LineTotal => Quantity * UnitPrice;

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
}
