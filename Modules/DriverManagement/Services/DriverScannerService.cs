using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Services
{
    public class DriverScannerService : IDriverScanner
    {
        public async Task<IReadOnlyList<Driver>> ScanInstalledDriversAsync()
        {
            return await Task.Run(() =>
            {
                var results = new List<Driver>();

                try
                {
                    var query = new SelectQuery("Win32_PnPSignedDriver");
                    using var searcher = new ManagementObjectSearcher(query);
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            var driver = new Driver
                            {
                                DeviceName = (obj["DeviceName"]?.ToString()) ?? (obj["FriendlyName"]?.ToString()) ?? string.Empty,
                                Vendor = obj["Manufacturer"]?.ToString(),
                                ProviderName = obj["ProviderName"]?.ToString(),
                                DriverVersion = obj["DriverVersion"]?.ToString(),
                                Status = obj["Status"]?.ToString(),
                                HardwareId = obj["HardwareId"] is string[] h ? string.Join(";", h) : obj["HardwareId"]?.ToString(),
                                PnpDeviceId = obj["DeviceID"]?.ToString(),
                                InfName = obj["InfName"]?.ToString(),
                                IsSigned = obj["IsSigned"] is bool b && b,
                            };

                            if (obj["DriverDate"] != null)
                            {
                                if (DateTime.TryParse(obj["DriverDate"].ToString(), out var dt))
                                {
                                    driver.DriverDate = dt;
                                }
                                else
                                {
                                    // Win32 may return yyyymmdd or other formats; attempt parsing manually
                                    var s = obj["DriverDate"].ToString();
                                    if (s.Length >= 8 && int.TryParse(s.Substring(0, 8), out _))
                                    {
                                        if (DateTime.TryParseExact(s.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var d2))
                                            driver.DriverDate = d2;
                                    }
                                }
                            }

                            driver.CreatedAt = DateTime.UtcNow;

                            results.Add(driver);
                        }
                        catch
                        {
                            // skip problematic record but continue scanning
                        }
                    }
                }
                catch (ManagementException mex)
                {
                    throw new InvalidOperationException("Failed to query Win32_PnPSignedDriver: " + mex.Message, mex);
                }

                return results;
            });
        }

        public async Task<Driver?> GetDriverByHardwareIdAsync(string hardwareId)
        {
            if (string.IsNullOrWhiteSpace(hardwareId)) return null;

            var drivers = await ScanInstalledDriversAsync();
            foreach (var d in drivers)
            {
                if (!string.IsNullOrWhiteSpace(d.HardwareId) && d.HardwareId.Contains(hardwareId, StringComparison.OrdinalIgnoreCase))
                    return d;
            }

            return null;
        }
    }
}
