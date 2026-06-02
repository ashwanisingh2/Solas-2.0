using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverBackupService
    {
        Task<DriverBackup> BackupAllDriversAsync(string destinationFolder);
        Task<DriverBackup> BackupSelectedDriversAsync(string destinationFolder, IEnumerable<int> driverIds);
        Task<IReadOnlyList<DriverBackup>> GetBackupHistoryAsync();
    }
}
