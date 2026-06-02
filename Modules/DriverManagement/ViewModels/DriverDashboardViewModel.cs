using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverDashboardViewModel : BaseViewModel
    {
        private readonly IDriverScanner _scanner;
        private readonly IDriverHealthAnalyzer _analyzer;

        public ObservableCollection<Driver> Drivers { get; } = new ObservableCollection<Driver>();

        private int _totalDrivers;
        public int TotalDrivers { get => _totalDrivers; set => SetProperty(ref _totalDrivers, value); }

        private int _healthyDrivers;
        public int HealthyDrivers { get => _healthyDrivers; set => SetProperty(ref _healthyDrivers, value); }

        private int _warningDrivers;
        public int WarningDrivers { get => _warningDrivers; set => SetProperty(ref _warningDrivers, value); }

        private int _criticalDrivers;
        public int CriticalDrivers { get => _criticalDrivers; set => SetProperty(ref _criticalDrivers, value); }

        private double _healthScore;
        public double HealthScore { get => _healthScore; set => SetProperty(ref _healthScore, value); }

        private bool _isRefreshing;
        public bool IsRefreshing { get => _isRefreshing; set => SetProperty(ref _isRefreshing, value); }

        public ICommand RefreshCommand { get; }

        public DriverDashboardViewModel(IDriverScanner scanner, IDriverHealthAnalyzer analyzer)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
        }

        private async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing) return;
            try
            {
                IsRefreshing = true;
                Drivers.Clear();
                var list = await _scanner.ScanInstalledDriversAsync();
                foreach (var d in list) Drivers.Add(d);

                var result = await _analyzer.AnalyzeAsync(list);
                TotalDrivers = result.Total;
                HealthyDrivers = result.Healthy;
                WarningDrivers = result.Warning;
                CriticalDrivers = result.Critical;
                HealthScore = result.HealthScore;
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}
