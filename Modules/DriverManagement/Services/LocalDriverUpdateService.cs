using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Services
{
    /// <summary>
    /// Simple local index-based driver update service. Looks for a JSON file at
    /// %LocalAppData%/Solas/driver_index.json with shape: { "hardwareId": "version", ... }
    /// This is a pluggable placeholder — can be replaced with an online/catalog implementation.
    /// </summary>
    public class LocalDriverUpdateService : IDriverUpdateService
    {
        private readonly Dictionary<string, string> _index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public LocalDriverUpdateService()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Solas", "driver_index.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var doc = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (doc != null)
                    {
                        foreach (var kv in doc) _index[kv.Key] = kv.Value;
                    }
                }
            }
            catch
            {
                // ignore index load failures; service will simply return no updates
            }
        }

        public Task<DriverUpdateInfo> CheckForUpdateAsync(Driver driver)
        {
            var result = new DriverUpdateInfo { HardwareId = driver.HardwareId };

            if (string.IsNullOrWhiteSpace(driver.HardwareId)) return Task.FromResult(result);

            try
            {
                var candidates = driver.HardwareId.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var c in candidates)
                {
                    if (_index.TryGetValue(c.Trim(), out var latest))
                    {
                        result.LatestVersion = latest;
                        result.Source = "local-index";
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(result.LatestVersion))
                {
                    // try vendor+device fallback key
                    var key = (driver.Vendor + "|" + driver.DeviceName).ToLowerInvariant();
                    if (_index.TryGetValue(key, out var lv))
                    {
                        result.LatestVersion = lv;
                        result.Source = "local-index";
                    }
                }

                // if we found a latest, compare semantically by simple string comparison
                if (!string.IsNullOrWhiteSpace(result.LatestVersion) && !string.IsNullOrWhiteSpace(driver.DriverVersion))
                {
                    result.LatestVersion = result.LatestVersion.Trim();
                }
            }
            catch
            {
                // ignore errors
            }

            return Task.FromResult(result);
        }
    }
}
