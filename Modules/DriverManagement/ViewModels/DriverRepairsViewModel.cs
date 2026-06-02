using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Infrastructure;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverRepairsViewModel : Infrastructure.BaseViewModel
    {
        private readonly IRepairService _repairService;
        private bool _isRunning;

        public DriverRepairsViewModel(IRepairService repairService)
        {
            _repairService = repairService;
            CreateRestorePointCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_restore_point.ps1"), () => !IsRunning);
            RunFullRepairCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_windows.ps1"), () => !IsRunning);
            RunSfcCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_sfc.ps1"), () => !IsRunning);
            RunDismCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_dism.ps1"), () => !IsRunning);
            ResetWindowsUpdateCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_windows_update.ps1"), () => !IsRunning);
            ResetNetworkCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_network.ps1"), () => !IsRunning);
            FlushDnsCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_dns.ps1"), () => !IsRunning);
            ResetPrinterCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_printer.ps1"), () => !IsRunning);
            CleanTempCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_temp_cleanup.ps1"), () => !IsRunning);
            AnalyzeBsodCommand = new AsyncRelayCommand(() => RunRepairAsync("analyze_bsod.ps1"), () => !IsRunning);
            DiagnoseBatteryCommand = new AsyncRelayCommand(() => RunRepairAsync("diagnostic_battery.ps1"), () => !IsRunning);
            ScanMalwareCommand = new AsyncRelayCommand(() => RunRepairAsync("scan_malware.ps1"), () => !IsRunning);
            CheckDiskHealthCommand = new AsyncRelayCommand(() => RunRepairAsync("check_disk_health.ps1"), () => !IsRunning);
            OptimizePerformanceCommand = new AsyncRelayCommand(() => RunRepairAsync("optimize_performance.ps1"), () => !IsRunning);
            RepairDriversCommand = new AsyncRelayCommand(() => RunRepairAsync("repair_drivers.ps1"), () => !IsRunning);
            OneClickCareCommand = new AsyncRelayCommand(() => RunRepairAsync("iobit_one_click_care.ps1"), () => !IsRunning);
        }

        public ICommand CreateRestorePointCommand { get; }
        public ICommand RunFullRepairCommand { get; }
        public ICommand RunSfcCommand { get; }
        public ICommand RunDismCommand { get; }
        public ICommand ResetWindowsUpdateCommand { get; }
        public ICommand ResetNetworkCommand { get; }
        public ICommand FlushDnsCommand { get; }
        public ICommand ResetPrinterCommand { get; }
        public ICommand CleanTempCommand { get; }
        public ICommand AnalyzeBsodCommand { get; }
        public ICommand DiagnoseBatteryCommand { get; }
        public ICommand ScanMalwareCommand { get; }
        public ICommand CheckDiskHealthCommand { get; }
        public ICommand OptimizePerformanceCommand { get; }
        public ICommand RepairDriversCommand { get; }
        public ICommand OneClickCareCommand { get; }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        private string _lastOutput = string.Empty;
        public string LastOutput
        {
            get => _lastOutput;
            set => SetProperty(ref _lastOutput, value);
        }

        private async Task RunRepairAsync(string scriptName)
        {
            if (IsRunning) return;

            try
            {
                IsRunning = true;
                LastOutput = $"Running {scriptName}...";
                var res = await _repairService.RunRepairAsync(scriptName);
                LastOutput = (res.Success ? "SUCCESS:\n" : "FAIL:\n") + res.Output + "\nERRORS:\n" + res.Error;
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void RaiseCommandStates()
        {
            ((AsyncRelayCommand)CreateRestorePointCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RunFullRepairCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RunSfcCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RunDismCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ResetWindowsUpdateCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ResetNetworkCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)FlushDnsCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ResetPrinterCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)CleanTempCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)AnalyzeBsodCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DiagnoseBatteryCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ScanMalwareCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)CheckDiskHealthCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)OptimizePerformanceCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RepairDriversCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)OneClickCareCommand).RaiseCanExecuteChanged();
        }
    }
}
