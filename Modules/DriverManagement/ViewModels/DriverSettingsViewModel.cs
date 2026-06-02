using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverSettingsViewModel : BaseViewModel
    {
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

        public ICommand SaveCommand { get; }
        public ICommand EnsureDatabaseCommand { get; }

        public DriverSettingsViewModel()
        {
            DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Solas", "driver_management.db");
            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverBackups");
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            EnsureDatabaseCommand = new AsyncRelayCommand(ExecuteEnsureDatabaseAsync);
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
                    // create empty SQLite database and apply schema if available
                    System.Data.SQLite.SQLiteConnection.CreateFile(DatabasePath);

                    // If schema.sql exists in module data, apply it
                    var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "DriverManagement", "Data", "schema.sql");
                    if (File.Exists(schemaPath))
                    {
                        var sql = File.ReadAllText(schemaPath);
                        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DatabasePath}");
                        await conn.OpenAsync();
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
