using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Bilnex.Pos.ViewModels;

namespace Bilnex.Pos.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
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

    private void RefocusBarcodeInput_Click(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(FocusBarcodeInput);
    }

    /// <summary>
    /// Yalnızca gerçek fare tıklamasında (programatik seçim değil) modalı açar.
    /// DataGridRow'a isabet edip etmediğini görsel ağaç üzerinden doğrular.
    /// </summary>
    private void BasketGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var hitRow = FindVisualAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (hitRow is null)
        {
            return;
        }

        // Satır zaten seçiliyse SelectedItem henüz değişmeyebilir;
        // BeginInvoke ile bir sonraki dispatch döngüsünde kontrol ederiz.
        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is PosViewModel vm && vm.SelectedItem is not null)
            {
                vm.OpenProductActionsModal();
            }
        });
    }

    private static T? FindVisualAncestor<T>(DependencyObject? obj) where T : DependencyObject
    {
        while (obj is not null)
        {
            if (obj is T match)
            {
                return match;
            }

            obj = VisualTreeHelper.GetParent(obj);
        }

        return null;
    }

    private void FocusBarcodeInput()
    {
        BarcodeTextBox.Focus();
        Keyboard.Focus(BarcodeTextBox);
        BarcodeTextBox.SelectAll();
    }
}
