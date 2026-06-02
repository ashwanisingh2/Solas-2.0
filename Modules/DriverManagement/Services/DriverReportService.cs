using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Services
{
    public class DriverReportService : IDriverReportService
    {
        private readonly string _databasePath;

        public DriverReportService(string databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        }

        public async Task<DriverReport> ExportCsvAsync(string destinationPath, IEnumerable<Driver> drivers)
        {
            var report = new DriverReport
            {
                FilePath = Path.GetFullPath(destinationPath),
                Format = "CSV"
            };

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,DeviceName,Vendor,ProviderName,DriverVersion,DriverDate,Status,HardwareId,PnpDeviceId,InfName,IsSigned");
                foreach (var d in drivers)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(d.Id.ToString()),
                        EscapeCsv(d.DeviceName),
                        EscapeCsv(d.Vendor),
                        EscapeCsv(d.ProviderName),
                        EscapeCsv(d.DriverVersion),
                        EscapeCsv(d.DriverDate?.ToString("o")),
                        EscapeCsv(d.Status),
                        EscapeCsv(d.HardwareId),
                        EscapeCsv(d.PnpDeviceId),
                        EscapeCsv(d.InfName),
                        EscapeCsv(d.IsSigned ? "1" : "0")
                    ));
                }

                await File.WriteAllTextAsync(report.FilePath, sb.ToString(), Encoding.UTF8);
                report.Success = true;
                await InsertReportRecordAsync(report);
                return report;
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.ParametersJson = JsonSerializer.Serialize(new { error = ex.ToString() });
                await InsertReportRecordAsync(report);
                return report;
            }
        }

        public async Task<DriverReport> ExportJsonAsync(string destinationPath, IEnumerable<Driver> drivers)
        {
            var report = new DriverReport
            {
                FilePath = Path.GetFullPath(destinationPath),
                Format = "JSON"
            };

            try
            {
                var json = JsonSerializer.Serialize(drivers, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(report.FilePath, json, Encoding.UTF8);
                report.Success = true;
                await InsertReportRecordAsync(report);
                return report;
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.ParametersJson = JsonSerializer.Serialize(new { error = ex.ToString() });
                await InsertReportRecordAsync(report);
                return report;
            }
        }

        public async Task<DriverReport> ExportPdfAsync(string destinationPath, IEnumerable<Driver> drivers)
        {
            var report = new DriverReport
            {
                FilePath = Path.GetFullPath(destinationPath),
                Format = "PDF"
            };

            try
            {
                // Minimal PDF generation via simple text rendering. To produce a proper PDF, add a PDF library.
                // Here we create a simple plain-text representation and save with .pdf extension. Consumer warned.
                var sb = new StringBuilder();
                sb.AppendLine("Drivers Report");
                sb.AppendLine("Generated: " + DateTime.UtcNow.ToString("o"));
                sb.AppendLine();
                foreach (var d in drivers)
                {
                    sb.AppendLine(d.ToString());
                }

                await File.WriteAllTextAsync(report.FilePath, sb.ToString(), Encoding.UTF8);
                report.Success = true;
                await InsertReportRecordAsync(report);
                return report;
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.ParametersJson = JsonSerializer.Serialize(new { error = ex.ToString() });
                await InsertReportRecordAsync(report);
                return report;
            }
        }

        public async Task<IReadOnlyList<DriverReport>> GetReportsAsync()
        {
            var list = new List<DriverReport>();
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, GeneratedAt, Format, FilePath, ParametersJson FROM DriverReports ORDER BY GeneratedAt DESC";
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var r = new DriverReport
                {
                    Id = rdr.GetInt32(0),
                    GeneratedAt = DateTime.Parse(rdr.GetString(1)),
                    Format = rdr.GetString(2),
                    FilePath = rdr.GetString(3),
                    ParametersJson = rdr.IsDBNull(4) ? null : rdr.GetString(4)
                };
                list.Add(r);
            }

            return list;
        }

        private async Task InsertReportRecordAsync(DriverReport report)
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_databasePath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO DriverReports (GeneratedAt, Format, FilePath, ParametersJson) VALUES ($date, $format, $path, $params); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$date", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$format", report.Format);
            cmd.Parameters.AddWithValue("$path", report.FilePath);
            cmd.Parameters.AddWithValue("$params", report.ParametersJson ?? string.Empty);
            var res = await cmd.ExecuteScalarAsync();
            if (res != null && int.TryParse(res.ToString(), out var id)) report.Id = id;
        }

        private static string EscapeCsv(string? value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return '"' + value.Replace("\"", "\"\"") + '"';
            }
            return value;
        }
    }
}
