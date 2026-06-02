using System.Threading.Tasks;

namespace Modules.DriverManagement.Interfaces
{
    public interface IRepairService
    {
        Task<RepairResult> RunRepairAsync(string scriptName);
    }

    public class RepairResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
