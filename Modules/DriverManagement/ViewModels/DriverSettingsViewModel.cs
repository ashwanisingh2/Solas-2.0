using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;
using Modules.DriverManagement.Interfaces;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverSettingsViewModel : BaseViewModel
    {
        private readonly IRepairService _repairService;

        private string _databasePath;
        public string DatabasePath
        {
            get => _databasePath;
            set => SetProperty(ref _databasePath, value);
        }

        private string _backupFolder;
        public string BackupFolder
        {
            get => _backupFolder;
            set => SetProperty(ref _backupFolder, value);
        }

        private bool _isWeeklyCareScheduled;
        public bool IsWeeklyCareScheduled
        {
            get => _isWeeklyCareScheduled;
            set => SetProperty(ref _isWeeklyCareScheduled, value);
        }

        private string? _scheduleStatusMessage;
        public string? ScheduleStatusMessage
        {
            get => _scheduleStatusMessage;
            set => SetProperty(ref _scheduleStatusMessage, value);
        }

        private bool _isSchedulingBusy;
        public bool IsSchedulingBusy
        {
            get => _isSchedulingBusy;
            set
            {
                if (SetProperty(ref _isSchedulingBusy, value))
                {
                    ((AsyncRelayCommand)ScheduleCareCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)UnscheduleCareCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand EnsureDatabaseCommand { get; }
        public ICommand ScheduleCareCommand { get; }
        public ICommand UnscheduleCareCommand { get; }

        public DriverSettingsViewModel(IRepairService repairService)
        {
            _repairService = repairService ?? throw new ArgumentNullException(nameof(repairService));

            DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Solas", "driver_management.db");
            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverBackups");
            
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            EnsureDatabaseCommand = new AsyncRelayCommand(ExecuteEnsureDatabaseAsync);
            ScheduleCareCommand = new AsyncRelayCommand(ExecuteScheduleCareAsync, () => !IsSchedulingBusy);
            UnscheduleCareCommand = new AsyncRelayCommand(ExecuteUnscheduleCareAsync, () => !IsSchedulingBusy);

            // Check scheduled status in background
            Task.Run(async () =>
            {
                var scheduled = await IsTaskScheduledAsync();
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    IsWeeklyCareScheduled = scheduled;
                    ScheduleStatusMessage = scheduled 
                        ? "Weekly care scheduled: Sunday at 3:00 AM (SYSTEM context)." 
                        : "Weekly care is currently disabled.";
                });
            });
        }

        private async Task<bool> IsTaskScheduledAsync()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-ScheduledTask -TaskName SolasDriverRepair_WeeklyCare\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) return false;
                await proc.WaitForExitAsync();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteScheduleCareAsync()
        {
            if (IsSchedulingBusy) return;
            IsSchedulingBusy = true;
            ScheduleStatusMessage = "Scheduling weekly care task...";
            try
            {
                var res = await _repairService.RunRepairAsync("schedule_care.ps1");
                if (res.Success)
                {
                    IsWeeklyCareScheduled = true;
                    ScheduleStatusMessage = "Weekly care scheduled successfully: Sunday at 3:00 AM.";
                }
                else
                {
                    ScheduleStatusMessage = $"Failed to schedule care: {res.Error}";
                }
            }
            catch (Exception ex)
            {
                ScheduleStatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSchedulingBusy = false;
            }
        }

        private async Task ExecuteUnscheduleCareAsync()
        {
            if (IsSchedulingBusy) return;
            IsSchedulingBusy = true;
            ScheduleStatusMessage = "Removing weekly care task...";
            try
            {
                var res = await _repairService.RunRepairAsync("unschedule_care.ps1");
                if (res.Success)
                {
                    IsWeeklyCareScheduled = false;
                    ScheduleStatusMessage = "Weekly care task removed successfully.";
                }
                else
                {
                    ScheduleStatusMessage = $"Failed to remove task: {res.Error}";
                }
            }
            catch (Exception ex)
            {
                ScheduleStatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSchedulingBusy = false;
            }
        }

        private Task ExecuteSaveAsync()
        {
            // Persist settings to a simple local file
            var dir = Path.GetDirectoryName(DatabasePath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

            var cfgDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Solas");
            Directory.CreateDirectory(cfgDir);
            var cfgFile = Path.Combine(cfgDir, "driver_settings.json");
            var json = System.Text.Json.JsonSerializer.Serialize(new { DatabasePath, BackupFolder });
            File.WriteAllText(cfgFile, json);
            return Task.CompletedTask;
        }

        private async Task ExecuteEnsureDatabaseAsync()
        {
            try
            {
                var dbDir = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrWhiteSpace(dbDir)) Directory.CreateDirectory(dbDir);

                if (!File.Exists(DatabasePath))
                {
                    // Opening a Microsoft.Data.Sqlite connection creates the database file.
                    var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "DriverManagement", "Data", "schema.sql");
                    using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DatabasePath}");
                    await conn.OpenAsync();

                    if (File.Exists(schemaPath))
                    {
                        var sql = File.ReadAllText(schemaPath);
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = sql;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // swallow - UI can show errors via additional plumbing if desired
            }
        }
    }
}
