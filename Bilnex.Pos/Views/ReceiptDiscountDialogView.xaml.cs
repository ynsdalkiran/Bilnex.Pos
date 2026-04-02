using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Bilnex.Pos.ViewModels;
using Key = System.Windows.Input.Key;

namespace Bilnex.Pos.Views;

public partial class ReceiptDiscountDialogView : UserControl
{
    private INotifyPropertyChanged? _viewModel;

    public ReceiptDiscountDialogView()
    {
        InitializeComponent();
        DataContextChanged += ReceiptDiscountDialogView_DataContextChanged;
        Unloaded += ReceiptDiscountDialogView_Unloaded;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        AttachToViewModel();
        FocusActiveEditor();
    }

    private void ReceiptDiscountDialogView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachFromViewModel();
        AttachToViewModel();
        FocusActiveEditor();
    }

    private void ReceiptDiscountDialogView_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachFromViewModel();
    }

    private void AttachToViewModel()
    {
        _viewModel = DataContext as INotifyPropertyChanged;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void DetachFromViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel = null;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PosViewModel.ReceiptDiscountEditorMode)
            or nameof(PosViewModel.ReceiptDiscountInputTarget)
            or nameof(PosViewModel.IsReceiptDiscountScreenOpen))
        {
            FocusActiveEditor();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (DataContext is not PosViewModel vm)
        {
            return;
        }

        var digit = e.Key switch
        {
            Key.D0 or Key.NumPad0 => "0",
            Key.D1 or Key.NumPad1 => "1",
            Key.D2 or Key.NumPad2 => "2",
            Key.D3 or Key.NumPad3 => "3",
            Key.D4 or Key.NumPad4 => "4",
            Key.D5 or Key.NumPad5 => "5",
            Key.D6 or Key.NumPad6 => "6",
            Key.D7 or Key.NumPad7 => "7",
            Key.D8 or Key.NumPad8 => "8",
            Key.D9 or Key.NumPad9 => "9",
            Key.OemComma or Key.Decimal => ",",
            _ => null
        };

        if (digit is not null && vm.AppendReceiptDiscountNumpadCommand.CanExecute(digit))
        {
            vm.AppendReceiptDiscountNumpadCommand.Execute(digit);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back && vm.BackspaceReceiptDiscountNumpadCommand.CanExecute(null))
        {
            vm.BackspaceReceiptDiscountNumpadCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && vm.ApplyReceiptDiscountCommand.CanExecute(null))
        {
            vm.ApplyReceiptDiscountCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && vm.CloseReceiptDiscountCommand.CanExecute(null))
        {
            vm.CloseReceiptDiscountCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void FocusActiveEditor()
    {
        if (DataContext is not PosViewModel viewModel)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (viewModel.IsReceiptRoundModeSelected)
            {
                RoundModeButton.Focus();
                NumpadSevenButton.Focus();
                Keyboard.Focus(NumpadSevenButton);
                return;
            }

            if (viewModel.IsReceiptPresetModeSelected)
            {
                PresetModeButton.Focus();
                Keyboard.Focus(PresetModeButton);
                return;
            }

            DiscountModeButton.Focus();

            if (viewModel.ReceiptDiscountInputTarget == "rate")
            {
                RateInputButton.Focus();
                Keyboard.Focus(RateInputButton);
                return;
            }

            AmountInputButton.Focus();
            Keyboard.Focus(AmountInputButton);
        }, DispatcherPriority.Loaded);
    }
}
