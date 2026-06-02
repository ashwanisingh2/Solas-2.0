using System;
using System.Text.Json;

namespace Modules.DriverManagement.Models
{
    public class DriverReport
    {
        public int Id { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Format { get; set; } = "CSV"; // CSV, JSON, PDF
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ParametersJson { get; set; }

        public DriverReport()
        {
            GeneratedAt = DateTime.UtcNow;
        }

        public T? GetParameters<T>() where T : class
        {
            if (string.IsNullOrWhiteSpace(ParametersJson)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(ParametersJson);
            }
            catch
            {
                return null;
            }
        }

        public void SetParameters<T>(T parameters) where T : class
        {
            ParametersJson = JsonSerializer.Serialize(parameters);
        }
    }
}
