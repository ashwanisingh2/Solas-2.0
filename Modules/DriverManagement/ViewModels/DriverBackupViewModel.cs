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
    public class DriverBackupViewModel : BaseViewModel
    {
        private readonly IDriverBackupService _backupService;

        public ObservableCollection<DriverBackup> History { get; } = new ObservableCollection<DriverBackup>();

        private bool _isBackingUp;
        public bool IsBackingUp { get => _isBackingUp; set => SetProperty(ref _isBackingUp, value); }

        public ICommand BackupAllCommand { get; }
        public ICommand RefreshHistoryCommand { get; }

        public DriverBackupViewModel(IDriverBackupService backupService)
        {
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            BackupAllCommand = new AsyncRelayCommand(ExecuteBackupAllAsync);
            RefreshHistoryCommand = new AsyncRelayCommand(ExecuteRefreshHistoryAsync);
        }

        private async Task ExecuteBackupAllAsync()
        {
            if (IsBackingUp) return;
            try
            {
                IsBackingUp = true;
                var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DriverBackups", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
                var result = await _backupService.BackupAllDriversAsync(dest);
                History.Insert(0, result);
            }
            finally
            {
                IsBackingUp = false;
            }
        }

        private async Task ExecuteRefreshHistoryAsync()
        {
            var list = await _backupService.GetBackupHistoryAsync();
            History.Clear();
            foreach (var item in list) History.Add(item);
        }
    }
}
