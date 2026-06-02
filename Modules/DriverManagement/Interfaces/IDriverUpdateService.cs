using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverUpdateService
    {
        Task<DriverUpdateInfo> CheckForUpdateAsync(Driver driver);
    }
}
