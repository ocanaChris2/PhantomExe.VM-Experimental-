#if NET6_0_OR_GREATER
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace PhantomExe.Core.VM
{
    internal class AssemblyBuilderImpl : IAssemblyBuilder
    {
        public Assembly BuildAssembly(byte[] decrypted)
        {
            if (decrypted == null || decrypted.Length == 0)
            {
                throw new ArgumentException("Decrypted assembly data cannot be null or empty", nameof(decrypted));
            }
            
            try
            {
                // ✅ Use AsmResolver to load and validate the assembly
                using var stream = new MemoryStream(decrypted);
                var module = ModuleDefinition.FromBytes(decrypted);
                
                // Validate it's a valid .NET assembly
                if (module == null || string.IsNullOrEmpty(module.Name))
                {
                    throw new InvalidOperationException("Invalid assembly format");
                }
                
                // Write to memory and load
                using var outputStream = new MemoryStream();
                module.Write(outputStream);
                var assemblyBytes = outputStream.ToArray();
                
                // Load into current context
                return Assembly.Load(assemblyBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load assembly with AsmResolver: {ex.Message}", ex);
            }
        }
        
        public Type ResolveType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }
            
            return Type.GetType(fullName, throwOnError: false) ??
                   AssemblyLoadContext.Default.Assemblies
                       .Select(a => a.GetType(fullName))
                       .FirstOrDefault(t => t != null);
        }
    }
    
    public interface IAssemblyBuilder
    {
        Assembly BuildAssembly(byte[] decrypted);
        Type ResolveType(string fullName);
    }
}
#endif