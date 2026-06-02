using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverScanner
    {
        Task<IReadOnlyList<Driver>> ScanInstalledDriversAsync();
        Task<Driver?> GetDriverByHardwareIdAsync(string hardwareId);
    }
}
