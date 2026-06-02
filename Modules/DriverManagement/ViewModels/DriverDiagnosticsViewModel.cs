using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Infrastructure;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverDiagnosticsViewModel : BaseViewModel
    {
        private readonly IDriverDiagnosticsService _diagnosticsService;

        public ObservableCollection<DriverDiagnostics> Diagnostics { get; } = new ObservableCollection<DriverDiagnostics>();

        public ICommand AnalyzeCommand { get; }
        public ICommand RefreshHistoryCommand { get; }

        public DriverDiagnosticsViewModel(IDriverDiagnosticsService diagnosticsService)
        {
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            AnalyzeCommand = new AsyncRelayCommand(ExecuteAnalyzeAsync);
            RefreshHistoryCommand = new AsyncRelayCommand(ExecuteRefreshHistoryAsync);
        }

        private async Task ExecuteAnalyzeAsync()
        {
            var list = await _diagnosticsService.AnalyzeEventLogsAsync();
            Diagnostics.Clear();
            foreach (var d in list) Diagnostics.Add(d);
        }

        private async Task ExecuteRefreshHistoryAsync()
        {
            var list = await _diagnosticsService.GetDiagnosticsHistoryAsync();
            Diagnostics.Clear();
            foreach (var d in list) Diagnostics.Add(d);
        }
    }
}
