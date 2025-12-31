#if NET6_0_OR_GREATER
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.DotNet;
using AsmResolver.DotNet.Memory;
using PhantomExe.Core;
using System;
using System.IO;
using System.Linq;
using SystemReflection = System.Reflection;
//using Microsoft.VisualBasic;
//using System.ComponentModel.DataAnnotations;
//using System.Runtime.InteropServices.Marshalling;
//using System.Net.WebSockets;
//using System.Reflection.Metadata; // ✅ Alias to avoid conflicts

namespace PhantomExe.Protector
{
    public partial class Protector
    {
        private string ProtectWithAsmResolver(string inputPath, ProtectionConfig config)
        {
            var module = ModuleDefinition.FromFile(inputPath);
            var key = config.RuntimeKey ?? Crypto.GenerateKey();

            VirtualizeMethodsAsmResolver(module, config, key);
            EmbedRuntime(module, key);
            InjectStubImplementation(module);
            RemoveExternalReferences(module);

            var outputPath = GetOutputPath(inputPath, config);
            module.Write(outputPath);
            return outputPath;
        }

        private void VirtualizeMethodsAsmResolver(ModuleDefinition module, ProtectionConfig config, byte[] key)
        {
            if (!config.VirtualizeMethods) return;
            var metadataMap = new Translation.AsmResolverMetadataMap(module);

            foreach (var type in module.TopLevelTypes)
            {
                foreach (var method in type.Methods)
                {
                    if (method.CilMethodBody == null || IsExcluded(method)) continue;
                    var translator = new Translation.AsmResolverTranslator(method, metadataMap);
                    var vmCode = translator.Translate();
                    
                    // ✅ Use full namespace since AesGcmUtil is in Shared
                    var encrypted = PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(vmCode, key);
                    var resName = $"ivm_{Guid.NewGuid():N}";
                    module.Resources.Add(new ManifestResource(resName, ManifestResourceAttributes.Private, 
                        new AsmResolver.DataSegment(encrypted)));
                    ReplaceWithStubAsmResolver(method, resName, key);
                }
            }

            var metadata = metadataMap.GetEncryptedMetadataBlob(key);
            module.Resources.Add(new ManifestResource("ivm.metadata", ManifestResourceAttributes.Private,
                new AsmResolver.DataSegment(metadata)));
        }

        private bool IsExcluded(MethodDefinition m)
        {
            // ✅ AsmResolver doesn't have IsStaticConstructor property - check manually
            var isStaticCtor = m.IsConstructor && m.IsStatic;
            var nameStr = m.Name?.ToString() ?? "";
            
            return m.IsConstructor || isStaticCtor || m.IsRuntimeSpecialName ||
                   nameStr == "Main" || nameStr == "Main$" || nameStr.StartsWith("<Main>$");
        }

        private void ReplaceWithStubAsmResolver(MethodDefinition method, string resName, byte[] key)
        {
            var module = method.Module!;
            
            // ✅ Ensure Stub assembly is referenced in the module
            var stubAssemblyRef = EnsureStubAssemblyReference(module);
            
            var body = new CilMethodBody(method);
            var instructions = body.Instructions;
            
            instructions.Clear();
            instructions.Add(CilOpCodes.Ldstr, resName);
            instructions.Add(CilOpCodes.Ldc_I8, BitConverter.ToInt64(key, 0));
            instructions.Add(CilOpCodes.Ldc_I8, BitConverter.ToInt64(key, 8));
            
            // ✅ Create a type reference to IVMRuntime in the Stub assembly
            var stubTypeRef = new TypeReference(stubAssemblyRef, "PhantomExe.Stub", "IVMRuntime");
            var importedType = module.DefaultImporter.ImportType(stubTypeRef);
            
            // ✅ Create method signature
            var executeSignature = MethodSignature.CreateStatic(
                module.CorLibTypeFactory.Object,
                module.CorLibTypeFactory.String,
                module.CorLibTypeFactory.Int64,
                module.CorLibTypeFactory.Int64);
            
            // ✅ Create method reference - use importedType directly
            var executeMethodRef = new MemberReference(importedType, "Execute", executeSignature);
            
            instructions.Add(CilOpCodes.Call, executeMethodRef);
            
            // Pop result if method returns void
            if (method.Signature?.ReturnType.IsTypeOf("System", "Void") == true)
                instructions.Add(CilOpCodes.Pop);
                
            instructions.Add(CilOpCodes.Ret);
            
            method.CilMethodBody = body;
        }
        
        private AssemblyReference EnsureStubAssemblyReference(ModuleDefinition module)
        {
            // Check if Stub assembly is already referenced
            var stubRef = module.AssemblyReferences.FirstOrDefault(r => r.Name == "PhantomExe.Stub");
            
            if (stubRef == null)
            {
                // Get Stub assembly info
                var stubAssembly = typeof(PhantomExe.Stub.IVMRuntime).Assembly;
                var stubAssemblyName = stubAssembly.GetName();
                
                // Create assembly reference
                stubRef = new AssemblyReference(
                    stubAssemblyName.Name,
                    new Version(stubAssemblyName.Version?.Major ?? 1,
                               stubAssemblyName.Version?.Minor ?? 0,
                               stubAssemblyName.Version?.Build ?? 0,
                               stubAssemblyName.Version?.Revision ?? 0));
                
                module.AssemblyReferences.Add(stubRef);
            }
            
            return stubRef;
        }

        private void EmbedRuntime(ModuleDefinition module, byte[] key)
        {
            // Try multiple locations for Core DLL
            var searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.Core.dll"),
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.Core.modern.dll"),
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.VM.Core.net6.0.dll"),
                Path.Combine(AppContext.BaseDirectory, "..", "Core", "net6.0", "bin", "Release", "net6.0", "PhantomExe.VM.Core.net6.0.dll"),
                Path.Combine(AppContext.BaseDirectory, "..", "Core", "net6.0", "bin", "Debug", "net6.0", "PhantomExe.VM.Core.net6.0.dll")
            };

            string? corePath = null;
            foreach (var path in searchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    corePath = fullPath;
                    break;
                }
            }

            if (corePath == null)
                throw new FileNotFoundException($"PhantomExe.Core.dll missing. Searched in: {string.Join(", ", searchPaths)}");

            var coreBytes = File.ReadAllBytes(corePath);
            
            // ✅ Use full namespace
            var encrypted = PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(coreBytes, key);
            module.Resources.Add(new ManifestResource("ivm.runtime", ManifestResourceAttributes.Private,
                new AsmResolver.DataSegment(encrypted)));
        }

        private void InjectStubImplementation(ModuleDefinition module)
        {
            var existingStub = module.TopLevelTypes.FirstOrDefault(t => t.FullName == "PhantomExe.Stub.IVMRuntime");
            if (existingStub != null) module.TopLevelTypes.Remove(existingStub);

            var stubType = new TypeDefinition("PhantomExe.Stub", "IVMRuntime",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit)
            {
                BaseType = module.CorLibTypeFactory.Object.Type
            };
            module.TopLevelTypes.Add(stubType);

            var executeMethod = new MethodDefinition("Execute",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodSignature.CreateStatic(
                    module.CorLibTypeFactory.Object,
                    module.CorLibTypeFactory.String,
                    module.CorLibTypeFactory.Int64,
                    module.CorLibTypeFactory.Int64));
            stubType.Methods.Add(executeMethod);

            CreateStubMethodBodyAsmResolver(executeMethod);
        }

        private void CreateStubMethodBodyAsmResolver(MethodDefinition method)
        {
            var module = method.Module!;
            
            // ✅ Ensure Stub assembly is referenced
            var stubAssemblyRef = EnsureStubAssemblyReference(module);
            
            var body = new CilMethodBody(method);
            var instructions = body.Instructions;
            
            instructions.Add(CilOpCodes.Ldarg_0);
            instructions.Add(CilOpCodes.Ldarg_1);
            instructions.Add(CilOpCodes.Ldarg_2);

            var executeSignature = MethodSignature.CreateStatic(
             module.CorLibTypeFactory.Object,
             module.CorLibTypeFactory.String,
            module.CorLibTypeFactory.Int64,
            module.CorLibTypeFactory.Int64);
            
            // ✅ Create type reference
            var stubTypeRef = new TypeReference(stubAssemblyRef, "PhantomExe.Stub", "IVMRuntime");
            //stubTypeRef = module.DefaultImporter.ImportType(stubTypeRef);
            var importedType = module.DefaultImporter.ImportType(stubTypeRef);
            var executeMethodRef = new MemberReference((TypeReference)importedType, "Execute", executeSignature);


            
        
            
            // ✅ Create method reference
            //var executeMethodRef = new MemberReference(stubTypeRef, "Execute", executeSignature);
            
            instructions.Add(CilOpCodes.Call, executeMethodRef);
            instructions.Add(CilOpCodes.Ret);
            
            method.CilMethodBody = body;
        }

        private void RemoveExternalReferences(ModuleDefinition module)
        {
            var toRemove = module.AssemblyReferences
                .Where(r => r.Name == "PhantomExe.Stub" || r.Name == "PhantomExe.Core")
                .ToList();
            foreach (var r in toRemove) module.AssemblyReferences.Remove(r);
        }
    }

    internal static class Crypto
    {
        public static byte[] GenerateKey() =>
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    }
}
#endif