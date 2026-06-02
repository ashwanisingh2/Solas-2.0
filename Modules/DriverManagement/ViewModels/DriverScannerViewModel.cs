using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverScannerViewModel : BaseViewModel
    {
        private readonly IDriverScanner _scanner;

        public ObservableCollection<Driver> Drivers { get; } = new ObservableCollection<Driver>();

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public ICommand ScanCommand { get; }

        public DriverScannerViewModel(IDriverScanner scanner)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            ScanCommand = new AsyncRelayCommand(ExecuteScanAsync);
        }

        private async Task ExecuteScanAsync()
        {
            if (IsScanning) return;
            try
            {
                IsScanning = true;
                Drivers.Clear();
                var list = await _scanner.ScanInstalledDriversAsync(true);
                foreach (var d in list)
                    Drivers.Add(d);
            }
            finally
            {
                IsScanning = false;
            }
        }
    }
}
