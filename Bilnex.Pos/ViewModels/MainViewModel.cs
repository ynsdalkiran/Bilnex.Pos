using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bilnex.Pos.Commands;
using Bilnex.Pos.Services;
using Bilnex.Pos.ViewModels.Base;
using Bilnex.Pos.Views;

namespace Bilnex.Pos.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly PosSettingsService _settingsService;
    private string _appTitle = "Bilnex POS Dashboard";
    private string _branchName = "Branch: Istanbul Kadikoy";
    private string _currentUser = "User: Admin";
    private UserControl _currentView;

    public MainViewModel()
    {
        _settingsService = PosSettingsService.Current;
        ThemeManager.ApplyTheme(_settingsService.Theme);
        _settingsService.SettingsChanged += OnSettingsChanged;

        PosSalesCommand = new RelayCommand(ShowPosView);
        CustomersCommand = new RelayCommand(ShowCustomersView);
        InventoryCommand = new RelayCommand(ShowInventoryView);
        CashCommand = new RelayCommand(() => ShowPlaceholder("Cash"));
        EndOfDayCommand = new RelayCommand(() => ShowPlaceholder("End of Day"));
        PriceChangeCommand = new RelayCommand(() => ShowPlaceholder("Price Change"));
        LabelPrintCommand = new RelayCommand(() => ShowPlaceholder("Label Print"));
        ProjectSettingsCommand = new RelayCommand(ShowProjectSettingsView);
        DashboardCommand = new RelayCommand(ShowDashboardView);
        ExitCommand = new RelayCommand(ExitApplication);

        _currentView = CreateDashboardView();
    }

    public string AppTitle
    {
        get => _appTitle;
        set => SetProperty(ref _appTitle, value);
    }

    public string BranchName
    {
        get => _branchName;
        set => SetProperty(ref _branchName, value);
    }

    public string CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public UserControl CurrentView
    {
        get => _currentView;
        private set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (SetProperty(ref _currentView, value))
            {
                OnPropertyChanged(nameof(IsDashboardViewActive));
            }
        }
    }

    public bool IsDashboardViewActive => CurrentView is DashboardView;

    public ICommand PosSalesCommand { get; }

    public ICommand CustomersCommand { get; }

    public ICommand InventoryCommand { get; }

    public ICommand CashCommand { get; }

    public ICommand EndOfDayCommand { get; }

    public ICommand PriceChangeCommand { get; }

    public ICommand LabelPrintCommand { get; }

    public ICommand ProjectSettingsCommand { get; }

    public ICommand DashboardCommand { get; }

    public ICommand ExitCommand { get; }

    private void ShowDashboardView()
    {
        CurrentView = CreateDashboardView();
    }

    private void ShowPosView()
    {
        CurrentView = CreatePosView();
    }

    private void ShowCustomersView()
    {
        CurrentView = CreateCustomersView();
    }

    private void ShowInventoryView()
    {
        CurrentView = CreateInventoryView();
    }

    private void ShowProjectSettingsView()
    {
        CurrentView = CreateProjectSettingsView();
    }

    private void ShowPlaceholder(string moduleName)
    {
        MessageBox.Show(
            $"{moduleName} screen will be added next.",
            "Navigation",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static void ExitApplication()
    {
        Application.Current.Shutdown();
    }

    private DashboardView CreateDashboardView()
    {
        return new DashboardView
        {
            DataContext = this
        };
    }

    private PosView CreatePosView()
    {
        return new PosView
        {
            DataContext = new PosViewModel()
        };
    }

    private CustomersView CreateCustomersView()
    {
        return new CustomersView
        {
            DataContext = this
        };
    }

    private InventoryView CreateInventoryView()
    {
        return new InventoryView
        {
            DataContext = this
        };
    }

    private ProjectSettingsView CreateProjectSettingsView()
    {
        return new ProjectSettingsView
        {
            DataContext = new ProjectSettingsViewModel()
        };
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ThemeManager.ApplyTheme(_settingsService.Theme);
            RefreshCurrentView();
        });
    }

    private void RefreshCurrentView()
    {
        CurrentView = CurrentView switch
        {
            DashboardView => CreateDashboardView(),
            PosView => CreatePosView(),
            CustomersView => CreateCustomersView(),
            InventoryView => CreateInventoryView(),
            ProjectSettingsView => CreateProjectSettingsView(),
            _ => CreateDashboardView()
        };
    }
}
