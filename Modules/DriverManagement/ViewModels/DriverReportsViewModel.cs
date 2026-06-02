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
        private readonly IDriverScanner _scanner;

        public ObservableCollection<DriverReport> Reports { get; } = new ObservableCollection<DriverReport>();

        private string? _statusMessage;
        public string? StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand RefreshCommand { get; }

        public DriverReportsViewModel(IDriverReportService reportService, IDriverScanner scanner)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            ExportCsvCommand = new AsyncRelayCommand(ExecuteExportCsvAsync);
            ExportJsonCommand = new AsyncRelayCommand(ExecuteExportJsonAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExecuteExportPdfAsync);
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
        }

        private async Task ExecuteExportCsvAsync()
        {
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverReports", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".csv");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var drivers = await _scanner.ScanInstalledDriversAsync(false);
            var result = await _reportService.ExportCsvAsync(dest, drivers);
            Reports.Insert(0, result);
            StatusMessage = result.Success ? $"Exported {drivers.Count} drivers to CSV." : "CSV export failed.";
        }

        private async Task ExecuteExportJsonAsync()
        {
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverReports", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var drivers = await _scanner.ScanInstalledDriversAsync(false);
            var result = await _reportService.ExportJsonAsync(dest, drivers);
            Reports.Insert(0, result);
            StatusMessage = result.Success ? $"Exported {drivers.Count} drivers to JSON." : "JSON export failed.";
        }

        private async Task ExecuteExportPdfAsync()
        {
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverReports", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var drivers = await _scanner.ScanInstalledDriversAsync(false);
            var result = await _reportService.ExportPdfAsync(dest, drivers);
            Reports.Insert(0, result);
            StatusMessage = result.Success ? $"Exported {drivers.Count} drivers to PDF." : "PDF export failed.";
        }

        private async Task ExecuteRefreshAsync()
        {
            var list = await _reportService.GetReportsAsync();
            Reports.Clear();
            foreach (var item in list) Reports.Add(item);
        }
    }
}
