using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.IO;
using System.Linq;

namespace PhantomExe.Protector.Detection
{
    public enum ProjectType
    {
        LegacyNetFramework,
        ModernNetCore,
        Unknown
    }

    public static class ProjectDetector
    {
        public static ProjectType Detect(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException(assemblyPath);

            // ✅ 1. .runtimeconfig.json = modern
            if (File.Exists(Path.ChangeExtension(assemblyPath, ".runtimeconfig.json")))
                return ProjectType.ModernNetCore;

            // ✅ 2. Reference analysis
            try
            {
                using var module = ModuleDefMD.Load(assemblyPath);
                var refs = module.GetTypes()
                    .SelectMany(t => t.Methods)
                    .Where(m => m.HasBody)
                    .SelectMany(m => m.Body.Instructions)
                    .Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
                    .Select(i => i.Operand as IMethod)
                    .Where(m => m != null)
                    .Select(m => m!.DeclaringType?.FullName) // ✅ Add null-forgiving operator
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .ToList();

                // ✅ Add null checks - use IndexOf for netstandard2.0 compatibility
                if (refs.Any(r => r != null && r.IndexOf("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) >= 0))
                    return ProjectType.ModernNetCore;

                if (refs.Any(r => r != null && r.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)))
                    return ProjectType.LegacyNetFramework;
            }
            catch { }

            return ProjectType.Unknown;
        }

        public static bool IsSplitModernApp(string exePath)
        {
            if (!File.Exists(exePath) || Path.GetExtension(exePath)?.ToLower() != ".exe")
                return false;

            var dir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(dir))
                return false;

            var name = Path.GetFileNameWithoutExtension(exePath);
            var dllPath = Path.Combine(dir, $"{name}.dll");

            return File.Exists(dllPath) && Detect(dllPath) == ProjectType.ModernNetCore;
        }
    }
}