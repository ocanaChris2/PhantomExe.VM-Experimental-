using System;
using System.IO;
using PhantomExe.Core;
using System.Reflection;

namespace PhantomExe.Stub
{
    public static class IVMRuntime
    {
        private static Assembly? _core;
        private static readonly object _lock = new();

        public static object? Execute(string resourceName, long keyHi, long keyLo)
        {
            var key = new byte[16];
            BitConverter.GetBytes(keyHi).CopyTo(key, 0);
            BitConverter.GetBytes(keyLo).CopyTo(key, 8);

            var core = LoadCore(key);
            var ctxType = core.GetType("PhantomExe.Core.VM.IVMContext")
                ?? throw new InvalidOperationException("IVMContext not found");

            var asm = Assembly.GetCallingAssembly();
            using var resStream = asm.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Resource {resourceName} missing");
            var enc = new byte[resStream.Length];
            resStream.Read(enc, 0, enc.Length);

            var ctx = Activator.CreateInstance(ctxType, resourceName, enc, key) as IDisposable;
            var result = ctxType.GetMethod("Execute")!.Invoke(ctx, new object[] { Array.Empty<object>() });
            ctx?.Dispose();
            return result;
        }

        private static Assembly LoadCore(byte[] key)
        {
            lock (_lock)
            {
                if (_core != null) return _core;
                var asm = Assembly.GetCallingAssembly();
                using var stream = asm.GetManifestResourceStream("ivm.runtime")
                    ?? throw new InvalidOperationException("ivm.runtime missing");
                var encrypted = new byte[stream.Length];
                stream.Read(encrypted, 0, encrypted.Length);
                var decrypted = PhantomExe.Core.Crypto.AesGcmUtil.Decrypt(encrypted, key);
                
                // ✅ Use reflection to find and invoke the builder without direct type reference
                var coreAsm = typeof(PhantomExe.Core.Crypto.AesGcmUtil).Assembly;
                var builderType = coreAsm.GetType("PhantomExe.Core.VM.AssemblyBuilderImpl")
                    ?? throw new InvalidOperationException("AssemblyBuilderImpl not found in Core runtime");
                
                var builder = Activator.CreateInstance(builderType)
                    ?? throw new InvalidOperationException("Failed to create AssemblyBuilderImpl");
                
                var buildMethod = builderType.GetMethod("BuildAssembly")
                    ?? throw new InvalidOperationException("BuildAssembly method not found");
                
                return _core = (Assembly)buildMethod.Invoke(builder, new object[] { decrypted })!;
            }
        }
    }
}