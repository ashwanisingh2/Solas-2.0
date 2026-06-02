using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;

namespace Modules.DriverManagement.Services
{
    public class DriverHealthAnalyzerService : IDriverHealthAnalyzer
    {
        public Task<DriverHealthResult> AnalyzeAsync(IReadOnlyList<Driver> drivers)
        {
            if (drivers == null) throw new ArgumentNullException(nameof(drivers));

            return Task.Run(() =>
            {
                int total = drivers.Count;
                int healthy = 0;
                int warning = 0;
                int critical = 0;

                foreach (var d in drivers)
                {
                    var classification = ClassifyDriver(d);
                    switch (classification)
                    {
                        case DriverSeverity.Healthy: healthy++; break;
                        case DriverSeverity.Warning: warning++; break;
                        case DriverSeverity.Critical: critical++; break;
                    }
                }

                double healthScore = ComputeHealthScore(total, healthy, warning, critical);

                return new DriverHealthResult
                {
                    Total = total,
                    Healthy = healthy,
                    Warning = warning,
                    Critical = critical,
                    HealthScore = Math.Round(healthScore, 2),
                    Summary = $"{healthy} healthy, {warning} warning, {critical} critical"
                };
            });
        }

        private DriverSeverity ClassifyDriver(Driver d)
        {
            // Missing driver: if DriverVersion is null/empty or HardwareId is empty
            if (string.IsNullOrWhiteSpace(d.DriverVersion) || string.IsNullOrWhiteSpace(d.HardwareId))
                return DriverSeverity.Critical;

            // Disabled: status contains 'Error' or 'Disabled' or 'Problem' -> Critical
            if (!string.IsNullOrWhiteSpace(d.Status))
            {
                var s = d.Status.ToLowerInvariant();
                if (s.Contains("error") || s.Contains("disabled") || s.Contains("problem"))
                    return DriverSeverity.Critical;
                if (s.Contains("unknown") || s.Contains("warning"))
                    return DriverSeverity.Warning;
            }

            // Unsigned drivers are warnings
            if (!d.IsSigned)
                return DriverSeverity.Warning;

            // Age-based heuristic: if driver older than 3 years -> warning
            if (d.DriverDate.HasValue)
            {
                var age = DateTime.UtcNow - d.DriverDate.Value;
                if (age.TotalDays > 365 * 3)
                    return DriverSeverity.Warning;
            }

            return DriverSeverity.Healthy;
        }

        private double ComputeHealthScore(int total, int healthy, int warning, int critical)
        {
            if (total == 0) return 100.0;

            // Start at 100, subtract weights
            double score = 100.0;
            score -= (warning * 5); // each warning reduces 5
            score -= (critical * 15); // each critical reduces 15

            // Clamp
            if (score < 0) score = 0;
            if (score > 100) score = 100;
            return score;
        }

        private enum DriverSeverity
        {
            Healthy,
            Warning,
            Critical
        }
    }
}
