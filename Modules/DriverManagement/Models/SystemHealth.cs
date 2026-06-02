namespace Modules.DriverManagement.Models
{
    public class SystemHealth
    {
        public double CpuPercentage { get; set; }
        public long TotalMemoryMb { get; set; }
        public long FreeMemoryMb { get; set; }
        public long DiskTotalMb { get; set; }
        public long DiskFreeMb { get; set; }
        public bool NetworkAvailable { get; set; }
        public int InstalledPrinters { get; set; }
        public bool DriversUpToDate { get; set; }
    }
}
