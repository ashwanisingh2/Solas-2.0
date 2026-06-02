using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Infrastructure;

namespace Modules.DriverManagement.ViewModels
{
    public class DriverRepairsViewModel : Infrastructure.BaseViewModel
    {
        private readonly IRepairService _repairService;

        public DriverRepairsViewModel(IRepairService repairService)
        {
            _repairService = repairService;
            RunFullRepairCommand = new AsyncRelayCommand(RunFullRepairAsync);
        }

        public ICommand RunFullRepairCommand { get; }

        private string _lastOutput = string.Empty;
        public string LastOutput
        {
            get => _lastOutput;
            set => SetProperty(ref _lastOutput, value);
        }

        private async Task RunFullRepairAsync()
        {
            var res = await _repairService.RunRepairAsync("repair_windows.ps1");
            LastOutput = (res.Success ? "SUCCESS:\n" : "FAIL:\n") + res.Output + "\nERRORS:\n" + res.Error;
        }
    }
}
