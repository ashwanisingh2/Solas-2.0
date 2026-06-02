using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDriverReportService
    {
        Task<DriverReport> ExportCsvAsync(string destinationPath, IEnumerable<Driver> drivers);
        Task<DriverReport> ExportJsonAsync(string destinationPath, IEnumerable<Driver> drivers);
        Task<DriverReport> ExportPdfAsync(string destinationPath, IEnumerable<Driver> drivers);
        Task<IReadOnlyList<DriverReport>> GetReportsAsync();
    }
}
