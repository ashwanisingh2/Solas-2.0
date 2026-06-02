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
    public class DriverRestoreViewModel : BaseViewModel
    {
        private readonly IDriverRestoreService _restoreService;

        public ObservableCollection<DriverRestore> History { get; } = new ObservableCollection<DriverRestore>();

        private bool _isRestoring;
        public bool IsRestoring { get => _isRestoring; set => SetProperty(ref _isRestoring, value); }

        public ICommand RestoreFromFolderCommand { get; }
        public ICommand RefreshHistoryCommand { get; }

        public DriverRestoreViewModel(IDriverRestoreService restoreService)
        {
            _restoreService = restoreService ?? throw new ArgumentNullException(nameof(restoreService));
            RestoreFromFolderCommand = new AsyncRelayCommand(ExecuteRestoreFromFolderAsync);
            RefreshHistoryCommand = new AsyncRelayCommand(ExecuteRefreshHistoryAsync);
        }

        private async Task ExecuteRestoreFromFolderAsync()
        {
            if (IsRestoring) return;
            try
            {
                IsRestoring = true;
                var src = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverBackups");
                var result = await _restoreService.RestoreDriversAsync(src, new int[0]);
                History.Insert(0, result);
            }
            finally
            {
                IsRestoring = false;
            }
        }

        private async Task ExecuteRefreshHistoryAsync()
        {
            var list = await _restoreService.GetRestoreHistoryAsync();
            History.Clear();
            foreach (var item in list) History.Add(item);
        }
    }
}
