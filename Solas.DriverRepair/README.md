Solas.DriverRepair — Windows Repair Tool (Module Staging)

Overview

This repository contains the Driver Management module and a WPF host project `Solas.DriverRepair` (C# .NET 8, WPF, MVVM) used as the starting point for a Windows Desktop Repair Tool for IT engineers.

Important
- This project targets Windows and must run on a real Windows machine (not Linux/macOS).
- Many repair operations require Administrator privileges (DISM, pnputil, Event Log access, PowerShell elevated commands).

Quick start (Windows)

Prerequisites
- .NET 8 SDK installed: https://dotnet.microsoft.com/
- Windows 10/11
- Recommended: Visual Studio 2022/2023 or use `dotnet` CLI

Build and run

Open a developer PowerShell or an elevated terminal (Run as Administrator if you plan to run repair actions):

```powershell
cd Solas-2.0/Solas.DriverRepair
dotnet build -c Release
dotnet run --project Solas.DriverRepair -c Release
```

What’s included
- Modules/DriverManagement — complete driver management module (models, services, viewmodels, views, data/schema).
- Solas.DriverRepair — WPF host project that wires DI and launches `Modules.DriverManagement.Views.MainWindow`.
- Modules/DriverManagement/Data/schema.sql — SQLite schema used by services.

Runtime notes
- Database path: Local AppData\Solas\driver_management.db by default. You can change this in `App.xaml.cs` or via `DriverSettingsView`.
- Backup/Restore operations call system tools (`dism.exe`, `pnputil.exe`) and must run elevated.
- Diagnostics reads Windows Event Logs (requires appropriate permissions).

Logs and Reports
- Backup/Restore/Diagnostics/Reports write logs and records to the SQLite DB. Files created by backup/report operations are placed under the user's Documents folder by default (e.g., `DriverBackups`, `DriverReports`).

Extensibility and Safety
- All system-level operations are performed via explicit processes (no remote/fake APIs).
- The solution favors minimal, auditable commands. Review PowerShell or process invocations before running in production.

Next steps
- Run the app on a Windows VM or machine and use the UI to navigate to modules (Dashboard, Scanner, Health, Backup, Restore, Reports, Diagnostics, Settings).
- To implement the broader repair modules requested (Network, Printer, Windows Repair, One-Click Auto Fix), I will add modules and PowerShell scripts that perform detection, remediation, verification, and logging.

If you want, I can now:
- Add PowerShell repair scripts and C# wrappers for the additional modules (Network, Printer, Windows Repair, Software Repair, Log Analyzer, One-Click Auto Fix).
- Wire a thorough One-Click scan-and-repair orchestrator that logs actions and verification steps.

