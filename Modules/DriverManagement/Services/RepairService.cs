using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Modules.DriverManagement.Interfaces;

namespace Modules.DriverManagement.Services
{
    public class RepairService : IRepairService
    {
        private readonly string _dbPath;
        private readonly string _scriptsFolder;

        public RepairService(string dbPath)
        {
            _dbPath = dbPath;
            _scriptsFolder = Path.Combine(AppContext.BaseDirectory, "Modules", "DriverManagement", "Scripts");
        }

        private bool IsElevated()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public async Task<RepairResult> RunRepairAsync(string scriptName)
        {
            var result = new RepairResult();

            if (!IsElevated())
            {
                result.Success = false;
                result.Error = "Not running with administrative privileges. Please run the application as Administrator.";
                return result;
            }

            var scriptPath = Path.Combine(_scriptsFolder, scriptName);
            if (!File.Exists(scriptPath))
            {
                result.Success = false;
                result.Error = "Script not found: " + scriptPath;
                return result;
            }

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi)!;
            if (proc == null)
            {
                result.Success = false;
                result.Error = "Failed to start PowerShell process.";
                return result;
            }

            sbOut.AppendLine(await proc.StandardOutput.ReadToEndAsync());
            sbErr.AppendLine(await proc.StandardError.ReadToEndAsync());

            proc.WaitForExit();

            result.Output = sbOut.ToString();
            result.Error = sbErr.ToString();
            result.Success = proc.ExitCode == 0;

            // Persist repair record and output
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = _dbPath };
                using var conn = new SqliteConnection(csb.ToString());
                await conn.OpenAsync();

                // Insert Repairs
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Repairs (Module, Action, Success, Summary, Log) VALUES ($m, $a, $s, $sum, $log); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$m", "WindowsRepair");
                cmd.Parameters.AddWithValue("$a", scriptName);
                cmd.Parameters.AddWithValue("$s", result.Success ? 1 : 0);
                cmd.Parameters.AddWithValue("$sum", result.Success ? "Completed" : "Failed");
                cmd.Parameters.AddWithValue("$log", result.Output + "\nERRORS:\n" + result.Error);
                var idObj = await cmd.ExecuteScalarAsync();
                var repairId = (long)idObj;

                // Add a single RepairLog entry with the full output
                using var logCmd = conn.CreateCommand();
                logCmd.CommandText = "INSERT INTO RepairLogs (RepairId, StepOrder, StepName, Success, Output) VALUES ($rid, $ord, $name, $s, $out);";
                logCmd.Parameters.AddWithValue("$rid", repairId);
                logCmd.Parameters.AddWithValue("$ord", 1);
                logCmd.Parameters.AddWithValue("$name", scriptName);
                logCmd.Parameters.AddWithValue("$s", result.Success ? 1 : 0);
                logCmd.Parameters.AddWithValue("$out", result.Output + "\nERRORS:\n" + result.Error);
                await logCmd.ExecuteNonQueryAsync();

                await conn.CloseAsync();
            }
            catch
            {
                // ignore persistence errors but surface outputs
            }

            return result;
        }
    }
}
