namespace Modules.DriverManagement.Models
{
    public class SoftwareUpdate
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
