using PhantomExe.Core;
using PhantomExe.Protector.Detection;
using System;
using System.IO;

namespace PhantomExe.Protector
{
    /// <summary>
    /// Hybrid protector: dnlib for netstandard2.0 (legacy), AsmResolver for net6.0+ (modern)
    /// </summary>
    public partial class Protector
    {
        private readonly IProgressReporter? _reporter;
        public Protector(IProgressReporter? reporter = null) => _reporter = reporter;

        public string Protect(string inputPath, ProtectionConfig config)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException(inputPath);

            ApplyTargetFrameworkSettings(config);
            var type = ProjectDetector.Detect(inputPath);
            _reporter?.Report($"🔍 Detected: {type}");

            return type switch
            {
#if NETSTANDARD2_0
                ProjectType.LegacyNetFramework => ProtectWithDnlib(inputPath, config),
                ProjectType.ModernNetCore => throw new NotSupportedException(
                    "Modern .NET Core protection requires net6.0+ build. Use the net6.0 version of this tool."),
#else
                ProjectType.LegacyNetFramework => ProtectWithAsmResolver(inputPath, config), // Can handle both
                ProjectType.ModernNetCore => ProjectDetector.IsSplitModernApp(inputPath)
                    ? ProtectSplitModern(inputPath, config)
                    : ProtectWithAsmResolver(inputPath, config),
#endif
                _ => throw new NotSupportedException($"Unsupported: {type}")
            };
        }

        private void ApplyTargetFrameworkSettings(ProtectionConfig config)
        {
            _reporter?.Report($"🎯 Target Framework: {config.TargetFramework}");
            switch (config.TargetFramework)
            {
                case TargetFramework.Net45:
                    config.EnableJIT = false;
                    break;
                case TargetFramework.Net5:
                case TargetFramework.Net6:
                case TargetFramework.Net7:
                case TargetFramework.Net8:
                case TargetFramework.Net9:
                    config.EnableJIT = true;
                    break;
            }
        }

#if NET6_0_OR_GREATER
        private string ProtectSplitModern(string exePath, ProtectionConfig config)
        {
            _reporter?.Report("📦 Modern split protection (EXE + DLL)");
            var dir = Path.GetDirectoryName(exePath)!;
            var name = Path.GetFileNameWithoutExtension(exePath);
            var dllPath = Path.Combine(dir, $"{name}.dll");
            var protectedDll = ProtectWithAsmResolver(dllPath, config);
            var finalDll = Path.Combine(dir, $"{name}.dll");
            if (File.Exists(finalDll)) File.Delete(finalDll);
            File.Move(protectedDll, finalDll);
            return exePath;
        }
#endif

        private string GetOutputPath(string input, ProtectionConfig config)
        {
            if (!string.IsNullOrEmpty(config.OutputPath))
                return config.OutputPath;
            
            var dir = Path.GetDirectoryName(input);
            if (string.IsNullOrEmpty(dir))
                dir = "."; // Fallback to current directory
            
            return Path.Combine(dir,
                $"{Path.GetFileNameWithoutExtension(input)}.ivm{Path.GetExtension(input)}");
        }
    }
}