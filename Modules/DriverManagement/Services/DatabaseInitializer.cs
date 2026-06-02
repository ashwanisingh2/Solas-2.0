using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Modules.DriverManagement.Interfaces;

namespace Modules.DriverManagement.Services
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly string _dbPath;
        private readonly string _schemaPath;

        public DatabaseInitializer(string dbPath)
        {
            _dbPath = dbPath;
            _schemaPath = Path.Combine(AppContext.BaseDirectory, "Modules", "DriverManagement", "Data", "schema.sql");
        }

        public async Task EnsureCreatedAsync()
        {
            var dir = Path.GetDirectoryName(_dbPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var connectionString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();

            // If DB file doesn't exist, run schema
            if (!File.Exists(_dbPath))
            {
                using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();
                var schema = File.Exists(_schemaPath) ? await File.ReadAllTextAsync(_schemaPath) : string.Empty;
                if (!string.IsNullOrWhiteSpace(schema))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = schema;
                    await cmd.ExecuteNonQueryAsync();
                }
                await conn.CloseAsync();
            }
        }
    }
}
