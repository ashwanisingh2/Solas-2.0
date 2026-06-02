using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Services
{
    public class DriverScannerService : IDriverScanner
    {
        private readonly object _cacheLock = new object();
        private IReadOnlyList<Driver>? _cache;
        private DateTime _cacheTimestamp = DateTime.MinValue;
        private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);

        public async Task<IReadOnlyList<Driver>> ScanInstalledDriversAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cache != null && (DateTime.UtcNow - _cacheTimestamp) < _cacheTtl)
            {
                return _cache;
            }

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
                                DeviceName = GetString(obj, "DeviceName") ?? GetString(obj, "FriendlyName") ?? string.Empty,
                                Vendor = GetString(obj, "Manufacturer"),
                                ProviderName = GetString(obj, "ProviderName"),
                                DriverVersion = GetString(obj, "DriverVersion"),
                                Status = GetString(obj, "Status") ?? "OK",
                                HardwareId = GetString(obj, "HardwareId"),
                                PnpDeviceId = GetString(obj, "DeviceID"),
                                InfName = GetString(obj, "InfName"),
                                IsSigned = GetBool(obj, "IsSigned"),
                            };

                            var driverDate = GetString(obj, "DriverDate");
                            if (driverDate != null)
                            {
                                if (DateTime.TryParse(driverDate, out var dt))
                                {
                                    driver.DriverDate = dt;
                                }
                                else
                                {
                                    var s = driverDate;
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

                // Query Win32_PnPEntity for devices with issues (ConfigManagerErrorCode > 0, e.g. Error 28 is Missing Driver)
                try
                {
                    var entityQuery = new SelectQuery("Win32_PnPEntity", "ConfigManagerErrorCode > 0");
                    using var entitySearcher = new ManagementObjectSearcher(entityQuery);
                    foreach (ManagementObject obj in entitySearcher.Get())
                    {
                        try
                        {
                            var errorCodeObj = obj["ConfigManagerErrorCode"];
                            if (errorCodeObj != null)
                            {
                                var errorCode = Convert.ToUInt32(errorCodeObj);
                                string statusStr = errorCode switch
                                {
                                    28 => "Missing Driver (Error 28)",
                                    1 => "Not Configured (Error 1)",
                                    10 => "Cannot Start (Error 10)",
                                    22 => "Disabled (Error 22)",
                                    _ => $"Device Error (Code {errorCode})"
                                };

                                var hardwareIdsObj = obj["HardwareID"] as string[];
                                string hardwareIdStr = hardwareIdsObj != null ? string.Join(";", hardwareIdsObj) : string.Empty;

                                var driver = new Driver
                                {
                                    DeviceName = GetString(obj, "Name") ?? GetString(obj, "Caption") ?? "Unknown Device",
                                    Vendor = GetString(obj, "Manufacturer") ?? "Unknown Vendor",
                                    ProviderName = "N/A",
                                    DriverVersion = "None",
                                    Status = statusStr,
                                    HardwareId = hardwareIdStr,
                                    PnpDeviceId = GetString(obj, "DeviceID"),
                                    InfName = string.Empty,
                                    IsSigned = false
                                };
                                driver.CreatedAt = DateTime.UtcNow;

                                // Prevent duplicate entries
                                if (!results.Any(r => r.PnpDeviceId == driver.PnpDeviceId))
                                {
                                    results.Add(driver);
                                }
                            }
                        }
                        catch
                        {
                            // ignore individual problem device errors
                        }
                    }
                }
                catch
                {
                    // ignore entire entity scan failure
                }

                lock (_cacheLock)
                {
                    _cache = results.AsReadOnly();
                    _cacheTimestamp = DateTime.UtcNow;
                }

                return _cache;
            });
        }

        public async Task<Driver?> GetDriverByHardwareIdAsync(string hardwareId)
        {
            if (string.IsNullOrWhiteSpace(hardwareId)) return null;
            var drivers = await ScanInstalledDriversAsync(false);
            foreach (var d in drivers)
            {
                if (!string.IsNullOrWhiteSpace(d.HardwareId) && d.HardwareId.Contains(hardwareId, StringComparison.OrdinalIgnoreCase))
                    return d;
            }

            return null;
        }

        private static string? GetString(ManagementObject obj, string propertyName)
        {
            try
            {
                var property = obj.Properties[propertyName];
                var value = property?.Value;
                return value switch
                {
                    null => null,
                    string[] values => string.Join(";", values),
                    _ => value.ToString()
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool GetBool(ManagementObject obj, string propertyName)
        {
            try
            {
                var value = obj.Properties[propertyName]?.Value;
                return value is bool b && b;
            }
            catch
            {
                return false;
            }
        }
    }
}
