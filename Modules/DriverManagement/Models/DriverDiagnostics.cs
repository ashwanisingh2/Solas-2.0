using System;

namespace Modules.DriverManagement.Models
{
    public class DriverDiagnostics
    {
        public int Id { get; set; }
        public DateTime EventDate { get; set; }
        public string? Level { get; set; }
        public string? Source { get; set; }
        public int? EventId { get; set; }
        public string? Message { get; set; }
        public string? DataJson { get; set; }

        public DriverDiagnostics() => EventDate = DateTime.UtcNow;
    }
}
