using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// ✅ Only include Windows APIs when targeting Windows
#if NETSTANDARD2_0 || NET6_0_OR_GREATER && WINDOWS
using Microsoft.Win32;
using System.Management;
using System.Net.NetworkInformation;
#endif

namespace PhantomExe.Core.Security
{
    public static class HardwareFingerprint
    {
        public static string Get()
        {
            var items = new[]
            {
                GetMachineGuid(),
                GetVolumeSerial(),
                GetMacAddress(),
                GetCpuId()
            }.Where(s => !string.IsNullOrEmpty(s));
            var hash = ComputeSha256(Encoding.UTF8.GetBytes(string.Join(";", items)));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string GetMachineGuid()
        {
            if (!IsWindows()) return "";
            try
            {
#if NETSTANDARD2_0 || NET6_0_OR_GREATER && WINDOWS
                using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
                return key?.GetValue("MachineGuid")?.ToString() ?? "";
#endif
            }
            catch { }
            return "";
        }

        private static string GetVolumeSerial()
        {
            if (!IsWindows()) return "";
            try
            {
#if NETSTANDARD2_0 || NET6_0_OR_GREATER && WINDOWS
                using var searcher = new ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_PhysicalMedia");
                using var collection = searcher.Get();
                foreach (ManagementBaseObject obj in collection)
                {
                    var serial = obj["VolumeSerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(serial)) return serial;
                }
#endif
            }
            catch { }
            return "";
        }

        private static string GetMacAddress()
        {
            try
            {
#if NETSTANDARD2_0 || NET6_0_OR_GREATER && WINDOWS
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault() ?? "";
#else
                return Environment.MachineName ?? "NonWindows";
#endif
            }
            catch { return ""; }
        }

        private static string GetCpuId()
        {
            if (!IsWindows()) return Environment.ProcessorCount.ToString();
            try
            {
#if NETSTANDARD2_0 || NET6_0_OR_GREATER && WINDOWS
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                using var collection = searcher.Get();
                foreach (ManagementBaseObject obj in collection)
                {
                    var id = obj["ProcessorId"]?.ToString();
                    if (!string.IsNullOrEmpty(id)) return id;
                }
#endif
            }
            catch { }
            return Environment.ProcessorCount.ToString();
        }

        private static bool IsWindows() =>
#if NET6_0_OR_GREATER
            OperatingSystem.IsWindows();
#else
            Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif

        private static byte[] ComputeSha256(byte[] data) =>
#if NET6_0_OR_GREATER
            SHA256.HashData(data);
#else
            SHA256.Create().ComputeHash(data);
#endif
    }
}