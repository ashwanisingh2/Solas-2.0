using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverDiagnosticsService
    {
        Task<IReadOnlyList<DriverDiagnostics>> AnalyzeEventLogsAsync();
        Task<IReadOnlyList<DriverDiagnostics>> GetDiagnosticsHistoryAsync();
    }
}
