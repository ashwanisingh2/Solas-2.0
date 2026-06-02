using System;
using Microsoft.Extensions.DependencyInjection;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Services;
using Modules.DriverManagement.ViewModels;

namespace Modules.DriverManagement.Infrastructure
{
    public static class DriverManagementModule
    {
        public static IServiceCollection AddDriverManagement(this IServiceCollection services, string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentNullException(nameof(databasePath));

            services.AddSingleton<IDriverScanner, Services.DriverScannerService>();
            services.AddSingleton<IDriverHealthAnalyzer, Services.DriverHealthAnalyzerService>();
            services.AddSingleton<IDriverBackupService>(sp => new DriverBackupService(databasePath));
            services.AddSingleton<IDriverRestoreService>(sp => new DriverRestoreService(databasePath));
            services.AddSingleton<IDriverReportService>(sp => new DriverReportService(databasePath));
            services.AddSingleton<IDriverDiagnosticsService>(sp => new DriverDiagnosticsService(databasePath));
            // Database initializer
            services.AddSingleton<Interfaces.IDatabaseInitializer>(sp => new Services.DatabaseInitializer(databasePath));
            // Repair orchestration
            services.AddSingleton<Interfaces.IRepairService>(sp => new Services.RepairService(databasePath));

            // ViewModels
            services.AddTransient<DriverDashboardViewModel>();
            services.AddTransient<DriverScannerViewModel>();
            services.AddTransient<DriverHealthViewModel>();
            services.AddTransient<DriverBackupViewModel>();
            services.AddTransient<DriverRestoreViewModel>();
            services.AddTransient<DriverReportsViewModel>();
            services.AddTransient<DriverDiagnosticsViewModel>();
            services.AddTransient<ViewModels.DriverRepairsViewModel>();
            services.AddSingleton<DriverSettingsViewModel>();

            return services;
        }
    }
}
