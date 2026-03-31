using System.Windows.Input;
using Bilnex.Pos.Commands;
using Bilnex.Pos.Services;
using Bilnex.Pos.ViewModels.Base;

namespace Bilnex.Pos.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel()
    {
        PosSalesCommand = new RelayCommand(() => NavigateTo("POS Sales"));
        CustomersCommand = new RelayCommand(() => NavigateTo("Customers"));
        InventoryCommand = new RelayCommand(() => NavigateTo("Inventory"));
        CashCommand = new RelayCommand(() => NavigateTo("Cash"));
        EndOfDayCommand = new RelayCommand(() => NavigateTo("End of Day"));
        ProjectSettingsCommand = new RelayCommand(() => NavigateTo("Project Settings"));
    }

    public ICommand PosSalesCommand { get; }

    public ICommand CustomersCommand { get; }

    public ICommand InventoryCommand { get; }

    public ICommand CashCommand { get; }

    public ICommand EndOfDayCommand { get; }

    public ICommand ProjectSettingsCommand { get; }

    private static void NavigateTo(string moduleName)
    {
        AppDialogService.ShowInfo(
            "Modül Açılışı",
            $"{moduleName} ekranı bu alanda açılacak.");
    }
}
