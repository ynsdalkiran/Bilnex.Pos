using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Bilnex.Pos.ViewModels;
using Key = System.Windows.Input.Key;

namespace Bilnex.Pos.Views;

public partial class LineItemActionDialogView : UserControl
{
    public LineItemActionDialogView()
    {
        InitializeComponent();
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

        if (digit is not null && vm.AppendLineItemActionNumpadCommand.CanExecute(digit))
        {
            vm.AppendLineItemActionNumpadCommand.Execute(digit);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back && vm.BackspaceLineItemActionNumpadCommand.CanExecute(null))
        {
            vm.BackspaceLineItemActionNumpadCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && vm.ApplyLineItemActionCommand.CanExecute(null))
        {
            vm.ApplyLineItemActionCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && vm.CloseLineItemActionDialogCommand.CanExecute(null))
        {
            vm.CloseLineItemActionDialogCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void FocusNumpad()
    {
        Dispatcher.BeginInvoke(() =>
        {
            NumpadFirstButton.Focus();
            Keyboard.Focus(NumpadFirstButton);
        }, DispatcherPriority.Loaded);
    }

    protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnIsKeyboardFocusWithinChanged(e);
        if (e.NewValue is true)
        {
            FocusNumpad();
        }
    }
}
