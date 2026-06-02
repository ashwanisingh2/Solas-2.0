using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;
using Microsoft.Data.Sqlite;

namespace Modules.DriverManagement.Services
{
    public class DriverDiagnosticsService : IDriverDiagnosticsService
    {
        private readonly string _databasePath;

        public DriverDiagnosticsService(string databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        }

        public async Task<IReadOnlyList<DriverDiagnostics>> AnalyzeEventLogsAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DriverDiagnostics>();

                try
                {
                    var query = "*[System[Provider[@Name='Microsoft-Windows-Kernel-PnP'] or Provider[@Name='Microsoft-Windows-UserPnp'] or EventID=219]]";
                    var q = new EventLogQuery("System", PathType.LogName, query);
                    using var reader = new EventLogReader(q);
                    for (EventRecord eventInstance = reader.ReadEvent(); eventInstance != null; eventInstance = reader.ReadEvent())
                    {
                        try
                        {
                            var diag = new DriverDiagnostics
                            {
                                EventDate = eventInstance.TimeCreated ?? DateTime.UtcNow,
                                Level = eventInstance.LevelDisplayName,
                                Source = eventInstance.ProviderName,
                                EventId = eventInstance.Id,
                                Message = eventInstance.FormatDescription(),
                                DataJson = null
                            };

                            list.Add(diag);

                            // Store into DB
                            _ = InsertDiagnosticRecordAsync(diag);
                        }
                        catch
                        {
                            // ignore record errors
                        }
                    }
                }
                catch (EventLogNotFoundException ex)
                {
                    throw new InvalidOperationException("Event log query failed: " + ex.Message, ex);
                }

                return list;
            });
        }

        public async Task<IReadOnlyList<DriverDiagnostics>> GetDiagnosticsHistoryAsync()
        {
            var list = new List<DriverDiagnostics>();
            using var conn = new SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, EventDate, Level, Source, EventId, Message, DataJson FROM DriverDiagnostics ORDER BY EventDate DESC";
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var d = new DriverDiagnostics
                {
                    Id = rdr.GetInt32(0),
                    EventDate = DateTime.Parse(rdr.GetString(1)),
                    Level = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                    Source = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    EventId = rdr.IsDBNull(4) ? null : (int?)rdr.GetInt32(4),
                    Message = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    DataJson = rdr.IsDBNull(6) ? null : rdr.GetString(6)
                };
                list.Add(d);
            }

            return list;
        }

        private async Task InsertDiagnosticRecordAsync(DriverDiagnostics diag)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_databasePath}");
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO DriverDiagnostics (EventDate, Level, Source, EventId, Message, DataJson) VALUES ($date, $level, $source, $eventId, $message, $data); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$date", diag.EventDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$level", diag.Level ?? string.Empty);
                cmd.Parameters.AddWithValue("$source", diag.Source ?? string.Empty);
                cmd.Parameters.AddWithValue("$eventId", diag.EventId ?? 0);
                cmd.Parameters.AddWithValue("$message", diag.Message ?? string.Empty);
                cmd.Parameters.AddWithValue("$data", diag.DataJson ?? string.Empty);
                var res = await cmd.ExecuteScalarAsync();
                if (res != null && int.TryParse(res.ToString(), out var id)) diag.Id = id;
            }
            catch
            {
                // swallow; diagnostics writing should not break functionality
            }
        }
    }
}
