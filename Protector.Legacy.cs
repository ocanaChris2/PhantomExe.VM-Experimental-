#if NETSTANDARD2_0
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using PhantomExe.Core;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PhantomExe.Protector
{
    public partial class Protector
    {
        private string ProtectWithDnlib(string inputPath, ProtectionConfig config)
        {
            var module = ModuleDefMD.Load(inputPath);
            var key = config.RuntimeKey ?? Crypto.GenerateKey();

            VirtualizeMethodsDnlib(module, config, key);
            EmbedRuntime(module, key);
            InjectStubImplementation(module);
            RemoveExternalReferences(module);

            var outputPath = GetOutputPath(inputPath, config);
            module.Write(outputPath);
            return outputPath;
        }

        private void VirtualizeMethodsDnlib(ModuleDef module, ProtectionConfig config, byte[] key)
        {
            if (!config.VirtualizeMethods) return;
            var metadataMap = new Translation.MetadataMap(module);

            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || IsExcluded(method)) continue;
                    var translator = new Translation.ILToVMTranslator(method, metadataMap);
                    var vmCode = translator.Translate();
                    var encrypted = PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(vmCode, key);
                    var resName = $"ivm_{Guid.NewGuid():N}";
                    module.Resources.Add(new EmbeddedResource(resName, encrypted));
                    ReplaceWithStubDnlib(method, resName, module, key);
                }
            }

            var metadata = metadataMap.GetEncryptedMetadataBlob(key);
            module.Resources.Add(new EmbeddedResource("ivm.metadata", metadata));
        }

        private bool IsExcluded(MethodDef m) =>
            m.IsConstructor || m.IsStaticConstructor || m.IsRuntimeSpecialName ||
            m.Name == "Main" || m.Name == "Main$" || m.Name.StartsWith("<Main>$");

        private void ReplaceWithStubDnlib(MethodDef method, string resName, ModuleDef module, byte[] key)
        {
            var body = new CilBody();
            var importer = new Importer(module);
            var stubType = module.Types.FirstOrDefault(t => t.FullName == "PhantomExe.Stub.IVMRuntime");
            var executeMethod = stubType?.Methods.FirstOrDefault(m => m.Name == "Execute")
                ?? throw new InvalidOperationException("Execute method not found");

            body.Instructions.Add(OpCodes.Ldstr.ToInstruction(resName));
            body.Instructions.Add(OpCodes.Ldc_I8.ToInstruction(BitConverter.ToInt64(key, 0)));
            body.Instructions.Add(OpCodes.Ldc_I8.ToInstruction(BitConverter.ToInt64(key, 8)));
            body.Instructions.Add(OpCodes.Call.ToInstruction(executeMethod));
            if (method.ReturnType.ElementType == ElementType.Void)
                body.Instructions.Add(OpCodes.Pop.ToInstruction());
            body.Instructions.Add(OpCodes.Ret.ToInstruction());

            body.KeepOldMaxStack = true;
            method.Body = body;
        }

        private void EmbedRuntime(ModuleDef module, byte[] key)
        {
            // Try multiple locations for Core DLL
            var searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.Core.dll"),
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.VM.Core.netstandard2.0.dll"),
                Path.Combine(AppContext.BaseDirectory, "..", "Core", "netstandard2.0", "bin", "Release", "netstandard2.0", "PhantomExe.VM.Core.netstandard2.0.dll"),
                Path.Combine(AppContext.BaseDirectory, "..", "Core", "netstandard2.0", "bin", "Debug", "netstandard2.0", "PhantomExe.VM.Core.netstandard2.0.dll")
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
            var encrypted = PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(coreBytes, key);
            module.Resources.Add(new EmbeddedResource("ivm.runtime", encrypted));
        }

        private void InjectStubImplementation(ModuleDef module)
        {
            var existingStub = module.Types.FirstOrDefault(t => t.FullName == "PhantomExe.Stub.IVMRuntime");
            if (existingStub != null) module.Types.Remove(existingStub);

            var stubType = new TypeDefUser("PhantomExe.Stub", "IVMRuntime",
                module.CorLibTypes.Object.ToTypeDefOrRef())
            {
                Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.Abstract |
                             dnlib.DotNet.TypeAttributes.Sealed | dnlib.DotNet.TypeAttributes.BeforeFieldInit
            };
            module.Types.Add(stubType);

            var executeSig = MethodSig.CreateStatic(module.CorLibTypes.Object,
                module.CorLibTypes.String, module.CorLibTypes.Int64, module.CorLibTypes.Int64);
            var executeMethod = new MethodDefUser("Execute", executeSig,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.HideBySig);
            stubType.Methods.Add(executeMethod);

            CreateStubMethodBodyDnlib(executeMethod, module, stubType);
        }

        private void CreateStubMethodBodyDnlib(MethodDef method, ModuleDef module, TypeDef stubType)
        {
            var body = new CilBody();
            var importer = new Importer(module);
            body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            body.Instructions.Add(OpCodes.Ldarg_2.ToInstruction());
            body.Instructions.Add(OpCodes.Call.ToInstruction(
                importer.Import(typeof(PhantomExe.Stub.IVMRuntime).GetMethod("Execute")!)));
            body.Instructions.Add(OpCodes.Ret.ToInstruction());

            body.MaxStack = 3;
            body.KeepOldMaxStack = true;
            TFMHelpers.TFMHelper.SimplifyMacros(body); // ✅ Only called in netstandard2.0
            method.Body = body;
        }

        private void RemoveExternalReferences(ModuleDef module)
        {
            var fieldNames = new[] { "assemblyRefs", "assemblyRefs2", "_assemblyRefs" };
            var refsList = (System.Collections.IList?)null;
            foreach (var name in fieldNames)
            {
                var field = typeof(ModuleDef).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) { refsList = (System.Collections.IList)field.GetValue(module); break; }
            }
            if (refsList == null) return;

            var toRemove = new System.Collections.Generic.List<AssemblyRef>();
            foreach (AssemblyRef r in refsList)
            {
                if (r.Name == "PhantomExe.Stub" || r.Name == "PhantomExe.Core")
                    toRemove.Add(r);
            }
            foreach (var r in toRemove) refsList.Remove(r);
        }
    }

    internal static class Crypto
    {
        public static byte[] GenerateKey()
        {
#if NET6_0_OR_GREATER
            return System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
#else
            var key = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
#endif
        }
    }
}
#endif