using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Modules.DriverManagement.Interfaces;
using Modules.DriverManagement.Models;
using System.IO;
using System.Drawing.Printing;

namespace Modules.DriverManagement.Services
{
    public class SystemHealthService : ISystemHealthService
    {
        public Task<SystemHealth> GetSystemHealthAsync(CancellationToken cancellationToken = default)
        {
            var info = new SystemHealth();
            // Memory via GlobalMemoryStatusEx
            try
            {
                var mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem))
                {
                    info.TotalMemoryMb = (long)(mem.ullTotalPhys / 1024 / 1024);
                    info.FreeMemoryMb = (long)(mem.ullAvailPhys / 1024 / 1024);
                }
                else
                {
                    info.TotalMemoryMb = -1;
                    info.FreeMemoryMb = -1;
                }
            }
            catch
            {
                info.TotalMemoryMb = -1;
                info.FreeMemoryMb = -1;
            }

            try
            {
                // Disk (sum of ready drives)
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                long total = 0, free = 0;
                foreach (var d in drives)
                {
                    try
                    {
                        total += d.TotalSize;
                        free += d.TotalFreeSpace;
                    }
                    catch { }
                }
                info.DiskTotalMb = total / 1024 / 1024;
                info.DiskFreeMb = free / 1024 / 1024;
            }
            catch
            {
                info.DiskTotalMb = -1;
                info.DiskFreeMb = -1;
            }

            try
            {
                info.NetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                info.NetworkAvailable = false;
            }

            try
            {
                info.InstalledPrinters = PrinterSettings.InstalledPrinters.Count;
            }
            catch
            {
                info.InstalledPrinters = 0;
            }

            // CPU sampling via GetSystemTimes (sample over short interval)
            try
            {
                if (TrySampleCpuPercentage(out var cpu))
                {
                    info.CpuPercentage = cpu;
                }
                else
                {
                    info.CpuPercentage = -1;
                }
            }
            catch
            {
                info.CpuPercentage = -1;
            }
            info.DriversUpToDate = true;

            return Task.FromResult(info);
        }
    }
}

    // P/Invoke helpers
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    internal static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    internal partial class SystemHealthService
    {
        private static bool GlobalMemoryStatusEx(MEMORYSTATUSEX ms)
        {
            return NativeMethods.GlobalMemoryStatusEx(ms);
        }

        private static long FileTimeToUInt64(FILETIME ft)
        {
            return ((long)ft.dwHighDateTime << 32) + ft.dwLowDateTime;
        }

        private static bool TrySampleCpuPercentage(out double cpuPercent)
        {
            cpuPercent = 0;
            if (!NativeMethods.GetSystemTimes(out var idle1, out var kernel1, out var user1)) return false;
            Thread.Sleep(500);
            if (!NativeMethods.GetSystemTimes(out var idle2, out var kernel2, out var user2)) return false;

            var idleTicks = FileTimeToUInt64(idle2) - FileTimeToUInt64(idle1);
            var kernelTicks = FileTimeToUInt64(kernel2) - FileTimeToUInt64(kernel1);
            var userTicks = FileTimeToUInt64(user2) - FileTimeToUInt64(user1);

            var systemTicks = kernelTicks + userTicks;
            if (systemTicks == 0) return false;

            var busy = systemTicks - idleTicks;
            cpuPercent = (busy * 100.0) / systemTicks;
            return true;
        }
    }
