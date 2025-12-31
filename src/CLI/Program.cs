using System;
using System.IO;
using PhantomExe.Core;
using PhantomExe.Protector;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║     PhantomExe — .NET VM Protector CLI         ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        // Parse command
        var command = args[0].ToLowerInvariant();
        
        if (command == "help" || command == "--help" || command == "-h" || command == "?")
        {
            ShowHelp();
            return 0;
        }

        if (command != "protect")
        {
            Console.Error.WriteLine($"❌ Unknown command: {command}");
            Console.Error.WriteLine("   Use 'protect' to protect an assembly, or 'help' for usage info.");
            return 1;
        }

        // Parse arguments
        if (args.Length < 2)
        {
            Console.Error.WriteLine("❌ Error: Input file required");
            Console.Error.WriteLine("   Usage: PhantomExe.CLI protect <input.dll> [options]");
            return 1;
        }

        var inputPath = args[1];
        
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"❌ File not found: {inputPath}");
            return 1;
        }

        // Parse options
        var config = new ProtectionConfig
        {
            EnableJIT = GetBoolOption(args, "--jit", true),
            EnableAntiDebug = GetBoolOption(args, "--anti-debug", true),
            EncryptStrings = GetBoolOption(args, "--encrypt-strings", true),
            ObfuscateControlFlow = GetBoolOption(args, "--obfuscate-flow", true),
            VirtualizeMethods = GetBoolOption(args, "--virtualize", true),
            TargetFramework = ParseTargetFramework(GetStringOption(args, "--target", "net9")),
            LicenseKey = GetStringOption(args, "--key", ""),
            OutputPath = GetStringOption(args, "--output", null)
        };

        try
        {
            Console.WriteLine($"🔒 Protecting: {Path.GetFileName(inputPath)}");
            Console.WriteLine($"🎯 Target: {config.TargetFramework}");
            Console.WriteLine($"⚙️  Options:");
            Console.WriteLine($"   • JIT: {config.EnableJIT}");
            Console.WriteLine($"   • Anti-Debug: {config.EnableAntiDebug}");
            Console.WriteLine($"   • Encrypt Strings: {config.EncryptStrings}");
            Console.WriteLine($"   • Obfuscate Flow: {config.ObfuscateControlFlow}");
            Console.WriteLine($"   • Virtualize: {config.VirtualizeMethods}");
            Console.WriteLine();

            var protector = new Protector();
            var outPath = protector.Protect(inputPath, config);

            Console.WriteLine();
            Console.WriteLine($"✅ Success! Protected: {Path.GetFileName(outPath)}");
            Console.WriteLine($"📁 Output: {outPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.Error.WriteLine($"❌ Protection failed: {ex.Message}");
            if (ex.InnerException != null)
                Console.Error.WriteLine($"   Details: {ex.InnerException.Message}");
            return 1;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  PhantomExe.CLI protect <input.dll> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <input.dll>              Input .NET assembly (.dll or .exe)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --output <path>          Output file path (default: input.ivm.dll)");
        Console.WriteLine("  --target <framework>     Target framework (net45, net5-9) [default: net9]");
        Console.WriteLine("  --key <license>          License key for hardware binding");
        Console.WriteLine("  --jit <true|false>       Enable JIT compilation [default: true]");
        Console.WriteLine("  --anti-debug <bool>      Enable anti-debugging [default: true]");
        Console.WriteLine("  --encrypt-strings <bool> Encrypt string constants [default: true]");
        Console.WriteLine("  --obfuscate-flow <bool>  Obfuscate control flow [default: true]");
        Console.WriteLine("  --virtualize <bool>      Virtualize methods [default: true]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  PhantomExe.CLI protect MyApp.dll");
        Console.WriteLine("  PhantomExe.CLI protect MyApp.dll --output Protected.dll --target net6");
        Console.WriteLine("  PhantomExe.CLI protect MyApp.dll --virtualize false --jit false");
    }

    static string GetStringOption(string[] args, string name, string? defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return defaultValue ?? "";
    }

    static bool GetBoolOption(string[] args, string name, bool defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                var value = args[i + 1];
                if (bool.TryParse(value, out var result))
                    return result;
            }
        }
        return defaultValue;
    }

    static TargetFramework ParseTargetFramework(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "net45" or "net4.5" => TargetFramework.Net45,
            "net5" or "net5.0" => TargetFramework.Net5,
            "net6" or "net6.0" => TargetFramework.Net6,
            "net7" or "net7.0" => TargetFramework.Net7,
            "net8" or "net8.0" => TargetFramework.Net8,
            "net9" or "net9.0" => TargetFramework.Net9,
            _ => TargetFramework.Net9
        };
    }
}