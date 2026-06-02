using System;

namespace Modules.DriverManagement.Models
{
    public class DriverHealthHistory
    {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public double HealthScore { get; set; }
        public string? Status { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? Details { get; set; }

        public DriverHealthHistory() => CheckedAt = DateTime.UtcNow;
    }
}
