using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverRestoreService
    {
        Task<DriverRestore> RestoreDriversAsync(string sourceFolder, IEnumerable<int> driverIds);
        Task<IReadOnlyList<DriverRestore>> GetRestoreHistoryAsync();
    }
}
