#if NETSTANDARD2_0
using System;
using System.Reflection;
using System.Linq;

namespace PhantomExe.Core.VM
{
    internal class AssemblyBuilderImpl : IAssemblyBuilder
    {
        public Assembly BuildAssembly(byte[] decrypted)
        {
            // ✅ For .NET Standard 2.0, use Assembly.Load directly
            // Reflection.Emit APIs are too limited in netstandard2.0
            
            if (decrypted == null || decrypted.Length == 0)
            {
                throw new ArgumentException("Decrypted assembly data cannot be null or empty", nameof(decrypted));
            }
            
            try
            {
                // Load the assembly from the byte array
                return Assembly.Load(decrypted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load assembly: {ex.Message}", ex);
            }
        }
        
        public Type ResolveType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }
            
            return Type.GetType(fullName) ??
                   AppDomain.CurrentDomain.GetAssemblies()
                       .Select(a => a.GetType(fullName))
                       .FirstOrDefault(t => t != null);
        }
    }
    
    internal interface IAssemblyBuilder
    {
        Assembly BuildAssembly(byte[] decrypted);
        Type ResolveType(string fullName);
    }
}
#endif