using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;
using Microsoft.Data.Sqlite;

namespace Modules.DriverManagement.Services
{
    public class DriverBackupService : IDriverBackupService
    {
        private readonly string _databasePath;

        public DriverBackupService(string databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        }

        public async Task<DriverBackup> BackupAllDriversAsync(string destinationFolder)
        {
            if (string.IsNullOrWhiteSpace(destinationFolder)) throw new ArgumentNullException(nameof(destinationFolder));

            Directory.CreateDirectory(destinationFolder);

            var backup = new DriverBackup
            {
                BackupPath = Path.GetFullPath(destinationFolder),
                BackupType = "All"
            };

            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = $"/Online /Export-Driver /Destination:\"{backup.BackupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null) throw new InvalidOperationException("Failed to start DISM process.");

                string stdout = await proc.StandardOutput.ReadToEndAsync();
                string stderr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                backup.Success = proc.ExitCode == 0;
                backup.Log = stdout + Environment.NewLine + stderr;

                // try to detect driver files count and set DriverIds as empty (we don't map drivers to ids here)
                backup.DriverIds = new List<int>();

                await InsertBackupRecordAsync(backup);

                return backup;
            }
            catch (Exception ex)
            {
                backup.Success = false;
                backup.Log = ex.ToString();
                await InsertBackupRecordAsync(backup);
                return backup;
            }
        }

        public async Task<DriverBackup> BackupSelectedDriversAsync(string destinationFolder, IEnumerable<int> driverIds)
        {
            if (string.IsNullOrWhiteSpace(destinationFolder)) throw new ArgumentNullException(nameof(destinationFolder));
            Directory.CreateDirectory(destinationFolder);

            var backup = new DriverBackup
            {
                BackupPath = Path.GetFullPath(destinationFolder),
                BackupType = "Selected",
                DriverIds = driverIds?.ToList() ?? new List<int>()
            };

            // For selected drivers, we need to export driver files for each driver INF. We'll perform a best-effort: for each driver id, look up INF name from DB and copy files.
            try
            {
                using var conn = new SqliteConnection($"Data Source={_databasePath}");
                await conn.OpenAsync();

                foreach (var id in backup.DriverIds)
                {
                    string inf = null;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT InfName FROM Drivers WHERE Id = $id LIMIT 1";
                        cmd.Parameters.AddWithValue("$id", id);
                        var res = await cmd.ExecuteScalarAsync();
                        if (res != null) inf = res.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(inf))
                    {
                        // Try to find INF under %windir%\inf and copy related files by parsing the INF file for CopyFiles sections is complex.
                        // Instead, use DISM's export-driver and then filter by INF presence: export all then pick files matching.
                        // Simpler approach: call DISM export to a temp folder and copy only drivers whose inf name appears in the exported INF files.
                    }
                }

                // As robust and deterministic approach, export all drivers to temp and then filter
                var temp = Path.Combine(Path.GetTempPath(), "driver_export_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temp);

                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/Online /Export-Driver /Destination:\"{temp}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc == null) throw new InvalidOperationException("Failed to start DISM process.");
                    string stdout = await proc.StandardOutput.ReadToEndAsync();
                    string stderr = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode != 0)
                    {
                        backup.Success = false;
                        backup.Log = stdout + Environment.NewLine + stderr;
                        await InsertBackupRecordAsync(backup);
                        return backup;
                    }
                }

                // Now, for each driver we want, attempt to match by INF name
                using (var filterConn = new SqliteConnection($"Data Source={_databasePath}"))
                {
                    await filterConn.OpenAsync();
                    foreach (var id in backup.DriverIds)
                    {
                        string inf = null;
                        using (var cmd = filterConn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT InfName FROM Drivers WHERE Id = $id LIMIT 1";
                            cmd.Parameters.AddWithValue("$id", id);
                            var res = await cmd.ExecuteScalarAsync();
                            if (res != null) inf = res.ToString();
                        }

                        if (string.IsNullOrWhiteSpace(inf)) continue;

                        // Find files in temp folder with matching inf name
                        var matches = Directory.EnumerateFiles(temp, "*.*", SearchOption.AllDirectories)
                            .Where(f => Path.GetFileName(f).Equals(inf, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(f).EndsWith(".inf", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(f).Equals(inf, StringComparison.OrdinalIgnoreCase));

                        foreach (var file in matches)
                        {
                            var destDir = Path.Combine(backup.BackupPath, Path.GetFileNameWithoutExtension(inf));
                            Directory.CreateDirectory(destDir);
                            var dest = Path.Combine(destDir, Path.GetFileName(file));
                            File.Copy(file, dest, true);
                        }
                    }
                }

                // Cleanup temp
                try { Directory.Delete(temp, true); } catch { }

                backup.Success = true;
                backup.Log = "Selected drivers exported successfully.";
                await InsertBackupRecordAsync(backup);
                return backup;
            }
            catch (Exception ex)
            {
                backup.Success = false;
                backup.Log = ex.ToString();
                await InsertBackupRecordAsync(backup);
                return backup;
            }
        }

        public async Task<IReadOnlyList<DriverBackup>> GetBackupHistoryAsync()
        {
            var list = new List<DriverBackup>();
            using var conn = new SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, BackupDate, BackupPath, DriverIdsJson, BackupType, Success, Log FROM DriverBackups ORDER BY BackupDate DESC";
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var b = new DriverBackup
                {
                    Id = rdr.GetInt32(0),
                    BackupDate = DateTime.Parse(rdr.GetString(1)),
                    BackupPath = rdr.GetString(2),
                    BackupType = rdr.GetString(4),
                    Success = rdr.GetInt32(5) == 1,
                    Log = rdr.IsDBNull(6) ? null : rdr.GetString(6)
                };
                var json = rdr.IsDBNull(3) ? null : rdr.GetString(3);
                if (!string.IsNullOrWhiteSpace(json)) b.LoadDriverIdsFromJson(json);
                list.Add(b);
            }

            return list;
        }

        private async Task InsertBackupRecordAsync(DriverBackup backup)
        {
            using var conn = new SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO DriverBackups (BackupDate, BackupPath, DriverIdsJson, BackupType, Success, Log) VALUES ($date, $path, $ids, $type, $success, $log); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$date", backup.BackupDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$path", backup.BackupPath);
            cmd.Parameters.AddWithValue("$ids", backup.DriverIdsJson());
            cmd.Parameters.AddWithValue("$type", backup.BackupType);
            cmd.Parameters.AddWithValue("$success", backup.Success ? 1 : 0);
            cmd.Parameters.AddWithValue("$log", backup.Log ?? string.Empty);
            var res = await cmd.ExecuteScalarAsync();
            if (res != null && int.TryParse(res.ToString(), out var id)) backup.Id = id;
        }
    }
}
