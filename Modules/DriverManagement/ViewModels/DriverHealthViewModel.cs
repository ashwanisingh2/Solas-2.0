using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverHealthViewModel : BaseViewModel
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

        private string? _summary;
        public string? Summary { get => _summary; set => SetProperty(ref _summary, value); }

        private bool _isAnalyzing;
        public bool IsAnalyzing { get => _isAnalyzing; set => SetProperty(ref _isAnalyzing, value); }

        public ICommand AnalyzeCommand { get; }

        public DriverHealthViewModel(IDriverScanner scanner, IDriverHealthAnalyzer analyzer)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            AnalyzeCommand = new AsyncRelayCommand(ExecuteAnalyzeAsync);
        }

        private async Task ExecuteAnalyzeAsync()
        {
            if (IsAnalyzing) return;
            try
            {
                IsAnalyzing = true;
                var list = await _scanner.ScanInstalledDriversAsync(true);
                Drivers.Clear();
                foreach (var driver in list) Drivers.Add(driver);

                var result = await _analyzer.AnalyzeAsync(list);
                TotalDrivers = result.Total;
                HealthyDrivers = result.Healthy;
                WarningDrivers = result.Warning;
                CriticalDrivers = result.Critical;
                HealthScore = result.HealthScore;
                Summary = result.Summary;
            }
            catch (Exception ex)
            {
                Summary = ex.Message;
            }
            finally
            {
                IsAnalyzing = false;
            }
        }
    }
}
