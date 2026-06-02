using System;

namespace Modules.DriverManagement.Models
{
    public class DriverScan
    {
        public int Id { get; set; }
        public DateTime ScanDate { get; set; }
        public int TotalDrivers { get; set; }
        public int HealthyCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public double HealthScore { get; set; }
        public string? ScannerVersion { get; set; }
        public string? Notes { get; set; }

        public DriverScan()
        {
            ScanDate = DateTime.UtcNow;
        }
    }
}
