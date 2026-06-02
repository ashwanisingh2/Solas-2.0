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
        private readonly ISystemHealthService _systemService;
        private readonly IDriverUpdateService _updateService;

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

        private double _cpuPercentage;
        public double CpuPercentage { get => _cpuPercentage; set => SetProperty(ref _cpuPercentage, value); }

        private long _totalMemoryMb;
        public long TotalMemoryMb
        {
            get => _totalMemoryMb;
            set
            {
                if (SetProperty(ref _totalMemoryMb, value))
                {
                    OnPropertyChanged(nameof(MemoryUsedPercentage));
                }
            }
        }

        private long _freeMemoryMb;
        public long FreeMemoryMb
        {
            get => _freeMemoryMb;
            set
            {
                if (SetProperty(ref _freeMemoryMb, value))
                {
                    OnPropertyChanged(nameof(MemoryUsedPercentage));
                }
            }
        }

        private long _diskTotalMb;
        public long DiskTotalMb
        {
            get => _diskTotalMb;
            set
            {
                if (SetProperty(ref _diskTotalMb, value))
                {
                    OnPropertyChanged(nameof(DiskUsedPercentage));
                }
            }
        }

        private long _diskFreeMb;
        public long DiskFreeMb
        {
            get => _diskFreeMb;
            set
            {
                if (SetProperty(ref _diskFreeMb, value))
                {
                    OnPropertyChanged(nameof(DiskUsedPercentage));
                }
            }
        }

        private bool _networkAvailable;
        public bool NetworkAvailable { get => _networkAvailable; set => SetProperty(ref _networkAvailable, value); }

        private int _installedPrinters;
        public int InstalledPrinters { get => _installedPrinters; set => SetProperty(ref _installedPrinters, value); }

        private bool _driversUpToDate;
        public bool DriversUpToDate { get => _driversUpToDate; set => SetProperty(ref _driversUpToDate, value); }

        public double MemoryUsedPercentage => TotalMemoryMb > 0 ? (double)(TotalMemoryMb - FreeMemoryMb) * 100 / TotalMemoryMb : 0;
        public double DiskUsedPercentage => DiskTotalMb > 0 ? (double)(DiskTotalMb - DiskFreeMb) * 100 / DiskTotalMb : 0;

        private bool _isRefreshing;
        public bool IsRefreshing { get => _isRefreshing; set => SetProperty(ref _isRefreshing, value); }

        private bool _autoRefresh;
        public bool AutoRefresh { get => _autoRefresh; set => SetProperty(ref _autoRefresh, value); }

        public ICommand RefreshCommand { get; }
        public ICommand StartAutoRefreshCommand { get; }
        public ICommand StopAutoRefreshCommand { get; }

        private System.Threading.Timer? _autoRefreshTimer;
        private readonly object _timerLock = new object();

        public DriverDashboardViewModel(IDriverScanner scanner, IDriverHealthAnalyzer analyzer, ISystemHealthService systemService, IDriverUpdateService updateService)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
            StartAutoRefreshCommand = new AsyncRelayCommand(StartAutoRefreshAsync, () => !AutoRefresh);
            StopAutoRefreshCommand = new AsyncRelayCommand(StopAutoRefreshAsync, () => AutoRefresh);
        }

        private async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing) return;
            try
            {
                IsRefreshing = true;
                Drivers.Clear();
                var list = await _scanner.ScanInstalledDriversAsync(true);
                foreach (var d in list) Drivers.Add(d);

                var result = await _analyzer.AnalyzeAsync(list);
                TotalDrivers = result.Total;
                HealthyDrivers = result.Healthy;
                WarningDrivers = result.Warning;
                CriticalDrivers = result.Critical;
                HealthScore = result.HealthScore;

                // Get system health metrics
                try
                {
                    var sys = await _systemService.GetSystemHealthAsync();
                    CpuPercentage = sys.CpuPercentage;
                    TotalMemoryMb = sys.TotalMemoryMb;
                    FreeMemoryMb = sys.FreeMemoryMb;
                    DiskTotalMb = sys.DiskTotalMb;
                    DiskFreeMb = sys.DiskFreeMb;
                    NetworkAvailable = sys.NetworkAvailable;
                    InstalledPrinters = sys.InstalledPrinters;
                    DriversUpToDate = sys.DriversUpToDate;
                }
                catch
                {
                    // ignore system health failures for now
                }

                // Check for driver updates (use local index); throttle parallelism
                try
                {
                    var tasks = list.Select(async d =>
                    {
                        try
                        {
                            var info = await _updateService.CheckForUpdateAsync(d);
                            d.LatestAvailableVersion = info.LatestVersion;
                            d.UpdateAvailable = !string.IsNullOrWhiteSpace(info.LatestVersion) && !string.Equals(info.LatestVersion, d.DriverVersion, StringComparison.OrdinalIgnoreCase);
                        }
                        catch { }
                    });

                    await Task.WhenAll(tasks);
                }
                catch { }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task StartAutoRefreshAsync()
        {
            AutoRefresh = true;
            // start timer to refresh every 60 seconds
            lock (_timerLock)
            {
                _autoRefreshTimer?.Dispose();
                _autoRefreshTimer = new System.Threading.Timer(async _ =>
                {
                    if (IsRefreshing) return;
                    try
                    {
                        await ExecuteRefreshAsync();
                    }
                    catch { }
                }, null, 0, 60000);
            }
            ((AsyncRelayCommand)StartAutoRefreshCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)StopAutoRefreshCommand).RaiseCanExecuteChanged();
            return Task.CompletedTask;
        }

        private Task StopAutoRefreshAsync()
        {
            AutoRefresh = false;
            lock (_timerLock)
            {
                _autoRefreshTimer?.Dispose();
                _autoRefreshTimer = null;
            }
            ((AsyncRelayCommand)StartAutoRefreshCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)StopAutoRefreshCommand).RaiseCanExecuteChanged();
            return Task.CompletedTask;
        }
    }
}
