using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverReportsViewModel : BaseViewModel
    {
        private readonly IDriverReportService _reportService;

        public ObservableCollection<DriverReport> Reports { get; } = new ObservableCollection<DriverReport>();

        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand RefreshCommand { get; }

        public DriverReportsViewModel(IDriverReportService reportService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            ExportCsvCommand = new AsyncRelayCommand(ExecuteExportCsvAsync);
            ExportJsonCommand = new AsyncRelayCommand(ExecuteExportJsonAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExecuteExportPdfAsync);
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
        }

        private async Task ExecuteExportCsvAsync()
        {
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverReports", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".csv");
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            // In a real scenario we'd pass selected drivers. For now export all drivers in DB
            var drivers = new Models.Driver[0];
            var result = await _reportService.ExportCsvAsync(dest, drivers);
            Reports.Insert(0, result);
        }

        private async Task ExecuteExportJsonAsync()
        {
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverReports", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            var drivers = new Models.Driver[0];
            var result = await _reportService.ExportJsonAsync(dest, drivers);
            Reports.Insert(0, result);
        }

        private async Task ExecuteExportPdfAsync()
        {
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverReports", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            var drivers = new Models.Driver[0];
            var result = await _reportService.ExportPdfAsync(dest, drivers);
            Reports.Insert(0, result);
        }

        private async Task ExecuteRefreshAsync()
        {
            var list = await _reportService.GetReportsAsync();
            Reports.Clear();
            foreach (var item in list) Reports.Add(item);
        }
    }
}
