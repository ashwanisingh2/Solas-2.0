using System;

namespace Modules.DriverManagement.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string? Vendor { get; set; }
        public string? ProviderName { get; set; }
        public string? DriverVersion { get; set; }
        public DateTime? DriverDate { get; set; }
        public string? Status { get; set; }
        public string? HardwareId { get; set; }
        public string? PnpDeviceId { get; set; }
        public string? InfName { get; set; }
        public bool IsSigned { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? LatestAvailableVersion { get; set; }
        public bool? UpdateAvailable { get; set; }

        public Driver() { }

        public override string ToString()
        {
            return $"{DeviceName} ({Vendor}) - {DriverVersion} - {HardwareId}";
        }
    }
}
