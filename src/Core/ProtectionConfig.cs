using System;

namespace PhantomExe.Core
{
    public enum TargetFramework
    {
        Net45, Net5, Net6, Net7, Net8, Net9
    }

    public class ProtectionConfig
    {
        public bool EnableJIT { get; set; } = true;
        public bool EnableAntiDebug { get; set; } = true;
        public bool EncryptStrings { get; set; } = true;
        public bool ObfuscateControlFlow { get; set; } = true;
        public bool VirtualizeMethods { get; set; } = true;
        public TargetFramework TargetFramework { get; set; } = TargetFramework.Net9;
        public string LicenseKey { get; set; } = "";
        public byte[]? RuntimeKey { get; set; }
        public string? OutputPath { get; set; }
    }
}