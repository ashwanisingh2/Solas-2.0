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
    public class DriverRestoreService : IDriverRestoreService
    {
        private readonly string _databasePath;

        public DriverRestoreService(string databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        }

        public async Task<DriverRestore> RestoreDriversAsync(string sourceFolder, IEnumerable<int> driverIds)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder)) throw new ArgumentNullException(nameof(sourceFolder));

            var restore = new DriverRestore
            {
                SourcePath = Path.GetFullPath(sourceFolder),
                DriverIds = driverIds?.ToList() ?? new List<int>()
            };

            try
            {
                // For each driver id, attempt to find INF in source folder and call pnputil /add-driver
                foreach (var id in restore.DriverIds)
                {
                    string inf = null;
                    using (var conn = new SqliteConnection($"Data Source={_databasePath}"))
                    {
                        await conn.OpenAsync();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT InfName FROM Drivers WHERE Id = $id LIMIT 1";
                        cmd.Parameters.AddWithValue("$id", id);
                        var res = await cmd.ExecuteScalarAsync();
                        if (res != null) inf = res.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(inf)) continue;

                    // search for inf under sourceFolder
                    var infFiles = Directory.EnumerateFiles(sourceFolder, inf, SearchOption.AllDirectories).ToList();
                    if (!infFiles.Any()) continue;

                    foreach (var infFile in infFiles)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "pnputil.exe",
                            Arguments = $"/add-driver \"{infFile}\" /install",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var proc = Process.Start(psi);
                        if (proc == null) throw new InvalidOperationException("Failed to start pnputil process.");
                        var stdout = await proc.StandardOutput.ReadToEndAsync();
                        var stderr = await proc.StandardError.ReadToEndAsync();
                        await proc.WaitForExitAsync();

                        restore.Log += stdout + Environment.NewLine + stderr + Environment.NewLine;
                    }
                }

                restore.Success = true;
                await InsertRestoreRecordAsync(restore);
                return restore;
            }
            catch (Exception ex)
            {
                restore.Success = false;
                restore.Log += ex.ToString();
                await InsertRestoreRecordAsync(restore);
                return restore;
            }
        }

        public async Task<IReadOnlyList<DriverRestore>> GetRestoreHistoryAsync()
        {
            var list = new List<DriverRestore>();
            using var conn = new SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, RestoreDate, SourcePath, DriverIdsJson, Success, Log FROM DriverRestores ORDER BY RestoreDate DESC";
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var r = new DriverRestore
                {
                    Id = rdr.GetInt32(0),
                    RestoreDate = DateTime.Parse(rdr.GetString(1)),
                    SourcePath = rdr.GetString(2),
                    Success = rdr.GetInt32(4) == 1,
                    Log = rdr.IsDBNull(5) ? null : rdr.GetString(5)
                };
                var json = rdr.IsDBNull(3) ? null : rdr.GetString(3);
                if (!string.IsNullOrWhiteSpace(json)) r.LoadDriverIdsFromJson(json);
                list.Add(r);
            }

            return list;
        }

        private async Task InsertRestoreRecordAsync(DriverRestore restore)
        {
            using var conn = new SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO DriverRestores (RestoreDate, SourcePath, DriverIdsJson, Success, Log) VALUES ($date, $path, $ids, $success, $log); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$date", restore.RestoreDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$path", restore.SourcePath);
            cmd.Parameters.AddWithValue("$ids", restore.DriverIdsJson());
            cmd.Parameters.AddWithValue("$success", restore.Success ? 1 : 0);
            cmd.Parameters.AddWithValue("$log", restore.Log ?? string.Empty);
            var res = await cmd.ExecuteScalarAsync();
            if (res != null && int.TryParse(res.ToString(), out var id)) restore.Id = id;
        }
    }
}
