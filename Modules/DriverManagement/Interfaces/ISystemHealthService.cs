using System.Threading;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface ISystemHealthService
    {
        Task<SystemHealth> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    }
}
