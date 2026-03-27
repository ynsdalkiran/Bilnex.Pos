using System.Windows.Controls;
using System.Windows.Input;
using Bilnex.Pos.ViewModels;

namespace Bilnex.Pos.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        FocusBarcodeInput();
    }

    private void BarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is PosViewModel viewModel)
            {
                viewModel.FinalizeBarcodeEntry();
            }

            Dispatcher.BeginInvoke(FocusBarcodeInput);
        }
    }

    private void BarcodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (DataContext is PosViewModel viewModel)
        {
            viewModel.RegisterBarcodeCharacter();
        }
    }

    private void RefocusBarcodeInput_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(FocusBarcodeInput);
    }

    private void FocusBarcodeInput()
    {
        BarcodeTextBox.Focus();
        Keyboard.Focus(BarcodeTextBox);
        BarcodeTextBox.SelectAll();
    }
}
