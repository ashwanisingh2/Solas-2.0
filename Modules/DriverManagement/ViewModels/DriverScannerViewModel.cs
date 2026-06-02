using System;
using System.Collections.Generic;
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
        private readonly IRepairService _repairService;

        public ObservableCollection<Driver> Drivers { get; } = new ObservableCollection<Driver>();
        public ObservableCollection<SoftwareUpdate> SoftwareUpdates { get; } = new ObservableCollection<SoftwareUpdate>();

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    ((AsyncRelayCommand)ScanCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)FixSelectedDriverCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private Driver? _selectedDriver;
        public Driver? SelectedDriver
        {
            get => _selectedDriver;
            set
            {
                if (SetProperty(ref _selectedDriver, value))
                {
                    ((AsyncRelayCommand)FixSelectedDriverCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Software updates properties
        private bool _isSoftwareScanning;
        public bool IsSoftwareScanning
        {
            get => _isSoftwareScanning;
            set
            {
                if (SetProperty(ref _isSoftwareScanning, value))
                {
                    ((AsyncRelayCommand)ScanSoftwareCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)UpdateSoftwareCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private SoftwareUpdate? _selectedSoftwareUpdate;
        public SoftwareUpdate? SelectedSoftwareUpdate
        {
            get => _selectedSoftwareUpdate;
            set
            {
                if (SetProperty(ref _selectedSoftwareUpdate, value))
                {
                    ((AsyncRelayCommand)UpdateSoftwareCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private string? _softwareStatusMessage;
        public string? SoftwareStatusMessage
        {
            get => _softwareStatusMessage;
            set => SetProperty(ref _softwareStatusMessage, value);
        }

        public ICommand ScanCommand { get; }
        public ICommand FixSelectedDriverCommand { get; }
        public ICommand ScanSoftwareCommand { get; }
        public ICommand UpdateSoftwareCommand { get; }

        public DriverScannerViewModel(IDriverScanner scanner, IRepairService repairService)
        {
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _repairService = repairService ?? throw new ArgumentNullException(nameof(repairService));

            ScanCommand = new AsyncRelayCommand(ExecuteScanAsync, () => !IsScanning);
            FixSelectedDriverCommand = new AsyncRelayCommand(ExecuteFixSelectedDriverAsync, () => SelectedDriver != null && !IsScanning);
            ScanSoftwareCommand = new AsyncRelayCommand(ExecuteScanSoftwareAsync, () => !IsSoftwareScanning);
            UpdateSoftwareCommand = new AsyncRelayCommand(ExecuteUpdateSoftwareAsync, () => SelectedSoftwareUpdate != null && !IsSoftwareScanning);
        }

        private async Task ExecuteScanAsync()
        {
            if (IsScanning) return;
            try
            {
                IsScanning = true;
                StatusMessage = "Scanning installed drivers...";
                Drivers.Clear();
                var list = await _scanner.ScanInstalledDriversAsync(true);
                foreach (var d in list)
                    Drivers.Add(d);
                StatusMessage = $"Scan completed. Found {list.Count} devices.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan failed: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task ExecuteFixSelectedDriverAsync()
        {
            if (SelectedDriver == null || string.IsNullOrWhiteSpace(SelectedDriver.PnpDeviceId)) return;
            try
            {
                IsScanning = true;
                StatusMessage = $"Attempting to reset device: {SelectedDriver.DeviceName}...";
                
                // Build argument - quoting the PnpDeviceId is crucial since it has ampersands and slashes
                var arg = $"-PnpDeviceId \"{SelectedDriver.PnpDeviceId}\"";
                var res = await _repairService.RunRepairAsync("repair_selected_driver.ps1", arg);
                
                if (res.Success)
                {
                    StatusMessage = $"Reset command executed successfully for {SelectedDriver.DeviceName}!";
                }
                else
                {
                    StatusMessage = $"Failed to reset device. Error: {res.Error}";
                }

                // Auto-refresh the scan results
                await ExecuteScanAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task ExecuteScanSoftwareAsync()
        {
            if (IsSoftwareScanning) return;
            try
            {
                IsSoftwareScanning = true;
                SoftwareStatusMessage = "Checking winget for available software upgrades...";
                SoftwareUpdates.Clear();

                var res = await _repairService.RunRepairAsync("scan_software_updates.ps1");
                if (res.Success && !string.IsNullOrWhiteSpace(res.Output))
                {
                    var cleanJson = res.Output.Trim();
                    int jsonStart = cleanJson.IndexOf('[');
                    if (jsonStart >= 0)
                    {
                        cleanJson = cleanJson.Substring(jsonStart);
                    }

                    var list = System.Text.Json.JsonSerializer.Deserialize<List<SoftwareUpdate>>(cleanJson, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (list != null && list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            SoftwareUpdates.Add(item);
                        }
                        SoftwareStatusMessage = $"Scan completed. Found {list.Count} software upgrades available.";
                    }
                    else
                    {
                        SoftwareStatusMessage = "All software is up to date (or winget output has no available upgrades).";
                    }
                }
                else
                {
                    SoftwareStatusMessage = "No updates found or winget is not configured.";
                }
            }
            catch (Exception ex)
            {
                SoftwareStatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSoftwareScanning = false;
            }
        }

        private async Task ExecuteUpdateSoftwareAsync()
        {
            if (SelectedSoftwareUpdate == null || IsSoftwareScanning) return;
            try
            {
                IsSoftwareScanning = true;
                SoftwareStatusMessage = $"Updating {SelectedSoftwareUpdate.Name}...";

                var arg = $"-Id \"{SelectedSoftwareUpdate.Id}\"";
                var res = await _repairService.RunRepairAsync("update_software.ps1", arg);

                if (res.Success)
                {
                    SoftwareStatusMessage = $"Successfully updated {SelectedSoftwareUpdate.Name}!";
                    await ExecuteScanSoftwareAsync();
                }
                else
                {
                    SoftwareStatusMessage = $"Failed to update {SelectedSoftwareUpdate.Name}. Error: {res.Error}";
                }
            }
            catch (Exception ex)
            {
                SoftwareStatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSoftwareScanning = false;
            }
        }
    }
}
