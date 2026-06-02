using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Modules.DriverManagement.Models
{
    public class DriverRestore
    {
        public int Id { get; set; }
        public DateTime RestoreDate { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public List<int> DriverIds { get; set; } = new List<int>();
        public bool Success { get; set; }
        public string? Log { get; set; }

        public DriverRestore() => RestoreDate = DateTime.UtcNow;

        public string DriverIdsJson()
        {
            return JsonSerializer.Serialize(DriverIds);
        }

        public void LoadDriverIdsFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                DriverIds = new List<int>();
                return;
            }

            try
            {
                DriverIds = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                DriverIds = new List<int>();
            }
        }
    }
}
