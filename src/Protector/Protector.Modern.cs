#if NET6_0_OR_GREATER
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.DotNet;
using PhantomExe.Core;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace PhantomExe.Protector
{
    public partial class Protector
    {
        private string ProtectWithAsmResolver(string inputPath, ProtectionConfig config)
        {
            var module = ModuleDefinition.FromFile(inputPath);
            var key = config.RuntimeKey ?? Crypto.GenerateKey();

            // IMPORTANT: Inject stub BEFORE virtualization so methods can reference it
            InjectStubImplementation(module);
            VirtualizeMethodsAsmResolver(module, config, key);
            EmbedRuntime(module, key);
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
                // Skip the stub type we just injected
                if (type.FullName == "PhantomExe.Stub.IVMRuntime") continue;
                
                foreach (var method in type.Methods)
                {
                    if (method.CilMethodBody == null || IsExcluded(method)) continue;
                    var translator = new Translation.AsmResolverTranslator(method, metadataMap);
                    var vmCode = translator.Translate();
                    
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
            var isStaticCtor = m.IsConstructor && m.IsStatic;
            var nameStr = m.Name?.ToString() ?? "";
            
            return m.IsConstructor || isStaticCtor || m.IsRuntimeSpecialName ||
                   nameStr == "Main" || nameStr == "Main$" || nameStr.StartsWith("<Main>$");
        }

        private void ReplaceWithStubAsmResolver(MethodDefinition method, string resName, byte[] key)
        {
            var module = method.Module!;
            
            // Find the stub type in the current module
            var stubType = module.TopLevelTypes.FirstOrDefault(t => t.FullName == "PhantomExe.Stub.IVMRuntime");
            if (stubType == null)
                throw new InvalidOperationException("Stub type not found in module");
            
            var executeMethod = stubType.Methods.FirstOrDefault(m => m.Name == "Execute");
            if (executeMethod == null)
                throw new InvalidOperationException("Execute method not found in stub");
            
            var body = new CilMethodBody(method);
            var instructions = body.Instructions;
            
            instructions.Clear();
            instructions.Add(CilOpCodes.Ldstr, resName);
            instructions.Add(CilOpCodes.Ldc_I8, BitConverter.ToInt64(key, 0));
            instructions.Add(CilOpCodes.Ldc_I8, BitConverter.ToInt64(key, 8));
            instructions.Add(CilOpCodes.Call, executeMethod);
            
            // Pop result if method returns void
            if (method.Signature?.ReturnType.IsTypeOf("System", "Void") == true)
                instructions.Add(CilOpCodes.Pop);
                
            instructions.Add(CilOpCodes.Ret);
            
            method.CilMethodBody = body;
        }

        private void EmbedRuntime(ModuleDefinition module, byte[] key)
        {
            var searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.VM.Core.net6.0.dll"),
                Path.Combine(AppContext.BaseDirectory, "PhantomExe.Core.dll"),
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
            var encrypted = PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(coreBytes, key);
            module.Resources.Add(new ManifestResource("ivm.runtime", ManifestResourceAttributes.Private,
                new AsmResolver.DataSegment(encrypted)));
        }

        private void InjectStubImplementation(ModuleDefinition module)
        {
            // Remove existing stub if present
            var existingStub = module.TopLevelTypes.FirstOrDefault(t => t.FullName == "PhantomExe.Stub.IVMRuntime");
            if (existingStub != null) module.TopLevelTypes.Remove(existingStub);

            // First, embed required types from Shared (AesGcmUtil, etc.)
            EmbedSharedTypes(module);
            
            // Then load and clone the Stub
            var stubAssembly = typeof(PhantomExe.Stub.IVMRuntime).Assembly;
            var stubModule = ModuleDefinition.FromFile(stubAssembly.Location);
            
            var stubTypeDef = stubModule.TopLevelTypes.FirstOrDefault(t => t.Name == "IVMRuntime");
            if (stubTypeDef == null)
                throw new InvalidOperationException("IVMRuntime type not found in Stub assembly");
            
            // Clone the entire type into the target module
            var clonedStub = CloneType(stubTypeDef, module);
            module.TopLevelTypes.Add(clonedStub);
        }
        
        private void EmbedSharedTypes(ModuleDefinition module)
        {
            // Load the Shared assembly
            var sharedAssembly = typeof(PhantomExe.Core.Crypto.AesGcmUtil).Assembly;
            var sharedModule = ModuleDefinition.FromFile(sharedAssembly.Location);
            
            // Find and clone AesGcmUtil
            var aesGcmType = sharedModule.TopLevelTypes
                .FirstOrDefault(t => t.FullName == "PhantomExe.Core.Crypto.AesGcmUtil");
            
            if (aesGcmType != null && !module.TopLevelTypes.Any(t => t.FullName == aesGcmType.FullName))
            {
                _reporter?.Report("📦 Embedding AesGcmUtil...");
                var cloned = CloneType(aesGcmType, module);
                module.TopLevelTypes.Add(cloned);
            }
        }

        private TypeDefinition CloneType(TypeDefinition source, ModuleDefinition targetModule)
        {
            var cloned = new TypeDefinition(source.Namespace, source.Name, source.Attributes)
            {
                BaseType = targetModule.DefaultImporter.ImportType(source.BaseType)
            };

            // Clone all fields first (methods may reference them)
            foreach (var field in source.Fields)
            {
                var clonedField = new FieldDefinition(
                    field.Name,
                    field.Attributes,
                    targetModule.DefaultImporter.ImportTypeSignature(field.Signature!.FieldType));
                cloned.Fields.Add(clonedField);
            }

            // Clone all methods
            foreach (var method in source.Methods)
            {
                var clonedMethod = CloneMethod(method, targetModule, source, cloned);
                cloned.Methods.Add(clonedMethod);
            }

            return cloned;
        }

        private MethodDefinition CloneMethod(MethodDefinition source, ModuleDefinition targetModule, 
            TypeDefinition sourceType, TypeDefinition targetType)
        {
            var cloned = new MethodDefinition(
                source.Name,
                source.Attributes,
                targetModule.DefaultImporter.ImportMethodSignature(source.Signature!));

            if (source.CilMethodBody != null)
            {
                var body = new CilMethodBody(cloned);
                
                // Clone local variables
                foreach (var local in source.CilMethodBody.LocalVariables)
                {
                    body.LocalVariables.Add(new CilLocalVariable(
                        targetModule.DefaultImporter.ImportTypeSignature(local.VariableType)));
                }
                
                // First pass: clone all instructions
                var instructionMap = new Dictionary<CilInstruction, CilInstruction>();
                foreach (var instr in source.CilMethodBody.Instructions)
                {
                    var clonedInstr = CloneInstruction(instr, targetModule, sourceType, targetType);
                    instructionMap[instr] = clonedInstr;
                    body.Instructions.Add(clonedInstr);
                }
                
                // Second pass: fix branch targets
                foreach (var kvp in instructionMap)
                {
                    var sourceInstr = kvp.Key;
                    var clonedInstr = kvp.Value;
                    
                    // Fix single instruction operands (branches)
                    if (sourceInstr.Operand is CilInstructionLabel label)
                    {
                        var targetInstr = label.Instruction;
                        if (targetInstr != null && instructionMap.TryGetValue(targetInstr, out var newTarget))
                        {
                            clonedInstr.Operand = new CilInstructionLabel(newTarget);
                        }
                    }
                    // Fix switch operands (multiple branches)
                    else if (sourceInstr.Operand is IList<ICilLabel> labels)
                    {
                        var newLabels = new List<ICilLabel>();
                        foreach (var lbl in labels)
                        {
                            if (lbl is CilInstructionLabel instrLabel)
                            {
                                var targetInstr = instrLabel.Instruction;
                                if (targetInstr != null && instructionMap.TryGetValue(targetInstr, out var newTarget))
                                {
                                    newLabels.Add(new CilInstructionLabel(newTarget));
                                }
                            }
                        }
                        clonedInstr.Operand = newLabels;
                    }
                }
                
                // Clone exception handlers
                foreach (var handler in source.CilMethodBody.ExceptionHandlers)
                {
                    var clonedHandler = new CilExceptionHandler();
                    clonedHandler.HandlerType = handler.HandlerType;
                    
                    // Helper to get instruction from label
                    CilInstruction? GetInstruction(ICilLabel? label)
                    {
                        if (label is CilInstructionLabel instrLabel)
                            return instrLabel.Instruction;
                        return null;
                    }
                    
                    var tryStart = GetInstruction(handler.TryStart);
                    if (tryStart != null && instructionMap.TryGetValue(tryStart, out var newTryStart))
                        clonedHandler.TryStart = new CilInstructionLabel(newTryStart);
                    
                    var tryEnd = GetInstruction(handler.TryEnd);
                    if (tryEnd != null && instructionMap.TryGetValue(tryEnd, out var newTryEnd))
                        clonedHandler.TryEnd = new CilInstructionLabel(newTryEnd);
                    
                    var handlerStart = GetInstruction(handler.HandlerStart);
                    if (handlerStart != null && instructionMap.TryGetValue(handlerStart, out var newHandlerStart))
                        clonedHandler.HandlerStart = new CilInstructionLabel(newHandlerStart);
                    
                    var handlerEnd = GetInstruction(handler.HandlerEnd);
                    if (handlerEnd != null && instructionMap.TryGetValue(handlerEnd, out var newHandlerEnd))
                        clonedHandler.HandlerEnd = new CilInstructionLabel(newHandlerEnd);
                    
                    var filterStart = GetInstruction(handler.FilterStart);
                    if (filterStart != null && instructionMap.TryGetValue(filterStart, out var newFilterStart))
                        clonedHandler.FilterStart = new CilInstructionLabel(newFilterStart);
                    
                    if (handler.ExceptionType != null)
                        clonedHandler.ExceptionType = targetModule.DefaultImporter.ImportType(handler.ExceptionType);
                    
                    body.ExceptionHandlers.Add(clonedHandler);
                }
                
                cloned.CilMethodBody = body;
            }

            return cloned;
        }

        private CilInstruction CloneInstruction(CilInstruction source, ModuleDefinition targetModule,
            TypeDefinition sourceType, TypeDefinition targetType)
        {
            object? operand = source.Operand;
            
            try
            {
                // Import type/method/field references
                if (operand is ITypeDefOrRef type)
                {
                    // If it references the source type, reference the target type instead
                    if (type is TypeDefinition typeDef && typeDef == sourceType)
                        operand = targetType;
                    else
                        operand = targetModule.DefaultImporter.ImportType(type);
                }
                else if (operand is IMethodDefOrRef method)
                {
                    // If it references a method in the source type, reference the cloned method
                    if (method.DeclaringType is TypeDefinition declType && declType == sourceType)
                    {
                        var targetMethod = targetType.Methods.FirstOrDefault(m => 
                            m.Name == method.Name && 
                            SignaturesMatch(m.Signature, method.Signature));
                        if (targetMethod != null)
                            operand = targetMethod;
                        else
                            operand = targetModule.DefaultImporter.ImportMethod(method);
                    }
                    else
                    {
                        operand = targetModule.DefaultImporter.ImportMethod(method);
                    }
                }
                else if (operand is IFieldDescriptor field)
                {
                    // If it references a field in the source type, reference the cloned field
                    if (field.DeclaringType is TypeDefinition declType && declType == sourceType)
                    {
                        var targetField = targetType.Fields.FirstOrDefault(f => f.Name == field.Name);
                        if (targetField != null)
                            operand = targetField;
                        else
                            operand = targetModule.DefaultImporter.ImportField(field);
                    }
                    else
                    {
                        operand = targetModule.DefaultImporter.ImportField(field);
                    }
                }
                else if (operand is MethodSpecification methodSpec)
                {
                    // Handle generic method instantiations (like Array.Empty<T>())
                    operand = targetModule.DefaultImporter.ImportMethod(methodSpec);
                }
                else if (operand is TypeSignature typeSig)
                {
                    operand = targetModule.DefaultImporter.ImportTypeSignature(typeSig);
                }
                else if (operand is MethodSignature methodSig)
                {
                    operand = targetModule.DefaultImporter.ImportMethodSignature(methodSig);
                }
                else if (operand is StandAloneSignature standAlone)
                {
                    // StandAloneSignature contains a signature that needs to be imported
                    if (standAlone.Signature is MethodSignature standaloneMethodSig)
                    {
                        var importedSig = targetModule.DefaultImporter.ImportMethodSignature(standaloneMethodSig);
                        operand = new StandAloneSignature(importedSig);
                    }
                }
            }
            catch (Exception)
            {
                // If import fails, keep the original operand and let AsmResolver handle it
                // This is better than crashing the entire protection process
            }
            // Don't clone labels here - we'll fix them in the second pass

            return new CilInstruction(source.OpCode, operand);
        }
        
        private bool SignaturesMatch(MethodSignature? sig1, MethodSignature? sig2)
        {
            if (sig1 == null || sig2 == null) return sig1 == sig2;
            if (sig1.ParameterTypes.Count != sig2.ParameterTypes.Count) return false;
            
            for (int i = 0; i < sig1.ParameterTypes.Count; i++)
            {
                if (sig1.ParameterTypes[i].FullName != sig2.ParameterTypes[i].FullName)
                    return false;
            }
            
            return true;
        }

        private void RemoveExternalReferences(ModuleDefinition module)
        {
            // Remove references to Stub and Core assemblies
            var toRemove = module.AssemblyReferences
                .Where(r => r.Name == "PhantomExe.Stub" || 
                           r.Name == "PhantomExe.Core" ||
                           r.Name == "PhantomExe.Shared")
                .ToList();
            
            foreach (var r in toRemove)
            {
                _reporter?.Report($"🗑️ Removing external reference: {r.Name}");
                module.AssemblyReferences.Remove(r);
            }
            
            // Verify no dangling references remain
            var stubRefs = module.AssemblyReferences
                .Where(r => r.Name?.Contains("PhantomExe") == true)
                .ToList();
            
            if (stubRefs.Any())
            {
                _reporter?.Report($"⚠️ Warning: {stubRefs.Count} PhantomExe references still present");
            }
        }
    }

    internal static class Crypto
    {
        public static byte[] GenerateKey() =>
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    }
}
#endif