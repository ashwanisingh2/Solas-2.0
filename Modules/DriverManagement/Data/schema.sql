-- SQLite schema for Driver Management Module
-- Tables: Drivers, DriverScans, DriverBackups, DriverRestores, DriverReports, DriverHealthHistory, DriverDiagnostics

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Drivers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DeviceName TEXT NOT NULL,
    Vendor TEXT,
    ProviderName TEXT,
    DriverVersion TEXT,
    DriverDate TEXT,
    Status TEXT,
    HardwareId TEXT,
    PnpDeviceId TEXT,
    InfName TEXT,
    IsSigned INTEGER DEFAULT 0,
    CreatedAt TEXT DEFAULT (datetime('now')),
    UpdatedAt TEXT
);

CREATE INDEX IF NOT EXISTS IDX_Drivers_HardwareId ON Drivers(HardwareId);
CREATE INDEX IF NOT EXISTS IDX_Drivers_PnpDeviceId ON Drivers(PnpDeviceId);

CREATE TABLE IF NOT EXISTS DriverScans (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScanDate TEXT DEFAULT (datetime('now')),
    TotalDrivers INTEGER,
    HealthyCount INTEGER,
    WarningCount INTEGER,
    CriticalCount INTEGER,
    HealthScore REAL,
    ScannerVersion TEXT,
    Notes TEXT
);

CREATE INDEX IF NOT EXISTS IDX_DriverScans_ScanDate ON DriverScans(ScanDate);

CREATE TABLE IF NOT EXISTS DriverBackups (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BackupDate TEXT DEFAULT (datetime('now')),
    BackupPath TEXT NOT NULL,
    DriverIdsJson TEXT,
    BackupType TEXT NOT NULL, -- 'All' or 'Selected'
    Success INTEGER DEFAULT 0,
    Log TEXT
);

CREATE INDEX IF NOT EXISTS IDX_DriverBackups_BackupDate ON DriverBackups(BackupDate);

CREATE TABLE IF NOT EXISTS DriverRestores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RestoreDate TEXT DEFAULT (datetime('now')),
    SourcePath TEXT NOT NULL,
    DriverIdsJson TEXT,
    Success INTEGER DEFAULT 0,
    Log TEXT
);

CREATE INDEX IF NOT EXISTS IDX_DriverRestores_RestoreDate ON DriverRestores(RestoreDate);

CREATE TABLE IF NOT EXISTS DriverReports (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GeneratedAt TEXT DEFAULT (datetime('now')),
    Format TEXT NOT NULL, -- CSV, JSON, PDF
    FilePath TEXT NOT NULL,
    ParametersJson TEXT
);

CREATE INDEX IF NOT EXISTS IDX_DriverReports_GeneratedAt ON DriverReports(GeneratedAt);

CREATE TABLE IF NOT EXISTS DriverHealthHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DriverId INTEGER NOT NULL,
    HealthScore REAL,
    Status TEXT,
    CheckedAt TEXT DEFAULT (datetime('now')),
    Details TEXT,
    FOREIGN KEY (DriverId) REFERENCES Drivers(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IDX_DriverHealthHistory_DriverId ON DriverHealthHistory(DriverId);

CREATE TABLE IF NOT EXISTS DriverDiagnostics (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventDate TEXT DEFAULT (datetime('now')),
    Level TEXT,
    Source TEXT,
    EventId INTEGER,
    Message TEXT,
    DataJson TEXT
);

CREATE INDEX IF NOT EXISTS IDX_DriverDiagnostics_EventDate ON DriverDiagnostics(EventDate);

-- A lightweight table to record application-level metadata
CREATE TABLE IF NOT EXISTS DM_Metadata (
    Key TEXT PRIMARY KEY,
    Value TEXT
);

-- Ensure foreign keys and journal mode are set for production usage via connection
-- Note: Enable WAL mode at runtime for better concurrency if desired:
-- PRAGMA journal_mode = WAL;

-- End of schema
