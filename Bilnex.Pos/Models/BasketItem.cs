using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bilnex.Pos.Models;

public sealed class BasketItem : INotifyPropertyChanged
{
    private string _productName = string.Empty;
    private int _quantity;
    private decimal _unitPrice;
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
            if (SetField(ref _unitPrice, value))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

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
