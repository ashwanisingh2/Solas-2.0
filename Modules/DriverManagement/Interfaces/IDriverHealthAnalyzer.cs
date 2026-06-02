using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverHealthAnalyzer
    {
        Task<DriverHealthResult> AnalyzeAsync(IReadOnlyList<Driver> drivers);
    }

    public class DriverHealthResult
    {
        public int Total { get; set; }
        public int Healthy { get; set; }
        public int Warning { get; set; }
        public int Critical { get; set; }
        public double HealthScore { get; set; }
        public string? Summary { get; set; }
    }
}
