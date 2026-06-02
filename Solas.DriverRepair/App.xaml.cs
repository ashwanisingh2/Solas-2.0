using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.DriverManagement.Infrastructure;

namespace Solas.DriverRepair
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Solas", "driver_management.db");

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDriverManagement(dbPath);
                    // Register MainWindow from module
                    services.AddSingleton<Modules.DriverManagement.Views.MainWindow>();
                })
                .Build();

            await _host.StartAsync();

            // Ensure database/schema exists
            try
            {
                var dbInit = _host.Services.GetService<Modules.DriverManagement.Interfaces.IDatabaseInitializer>();
                if (dbInit != null)
                {
                    await dbInit.EnsureCreatedAsync();
                }
            }
            catch
            {
                // ignore - application can still run but DB-backed features may fail
            }

            var mainWindow = _host.Services.GetRequiredService<Modules.DriverManagement.Views.MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}
