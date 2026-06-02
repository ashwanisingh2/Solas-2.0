namespace Modules.DriverManagement.Models
{
    public class DriverUpdateInfo
    {
        public string? HardwareId { get; set; }
        public string? LatestVersion { get; set; }
        public string? Source { get; set; }
        public bool IsLatest => string.IsNullOrWhiteSpace(LatestVersion);
    }
}
