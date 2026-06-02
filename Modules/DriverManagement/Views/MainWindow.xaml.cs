using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Modules.DriverManagement.Views.DriverBackup;
using Modules.DriverManagement.Views.DriverDashboard;
using Modules.DriverManagement.Views.DriverDiagnostics;
using Modules.DriverManagement.Views.DriverHealth;
using Modules.DriverManagement.Views.DriverReports;
using Modules.DriverManagement.Views.DriverRestore;
using Modules.DriverManagement.Views.DriverScanner;
using Modules.DriverManagement.Views.DriverSettings;

namespace Modules.DriverManagement.Views
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _services;

        public MainWindow(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
            WireButtons();
        }

        private void WireButtons()
        {
            BtnDashboard.Click += (_, __) => NavigateToDashboard();
            BtnScanner.Click += (_, __) => NavigateToScanner();
            BtnHealth.Click += (_, __) => NavigateToHealth();
            BtnBackup.Click += (_, __) => NavigateToBackup();
            BtnRestore.Click += (_, __) => NavigateToRestore();
            BtnReports.Click += (_, __) => NavigateToReports();
            BtnDiagnostics.Click += (_, __) => NavigateToDiagnostics();
            BtnSettings.Click += (_, __) => NavigateToSettings();
        }

        private void NavigateToDashboard()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverDashboardViewModel>();
            var view = new DriverDashboardView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToScanner()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverScannerViewModel>();
            var view = new DriverScannerView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToHealth()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverHealthViewModel>();
            var view = new DriverHealthView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToBackup()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverBackupViewModel>();
            var view = new DriverBackupView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToRestore()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverRestoreViewModel>();
            var view = new DriverRestoreView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToReports()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverReportsViewModel>();
            var view = new DriverReportsView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToDiagnostics()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverDiagnosticsViewModel>();
            var view = new DriverDiagnosticsView { DataContext = vm };
            MainHost.Content = view;
        }

        private void NavigateToSettings()
        {
            var vm = _services.GetRequiredService<ViewModels.DriverSettingsViewModel>();
            var view = new DriverSettingsView { DataContext = vm };
            MainHost.Content = view;
        }
    }
}
