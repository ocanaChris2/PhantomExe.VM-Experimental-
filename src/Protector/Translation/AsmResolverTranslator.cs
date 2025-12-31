// src/Protector/Translation/AsmResolverTranslator.cs
#if NET6_0_OR_GREATER
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables; // ✅ ADD THIS for ElementType
using PhantomExe.Core.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PhantomExe.Protector.Translation
{
    /// <summary>
    /// Translates CIL to PhantomExe VM bytecode using AsmResolver (net6.0+)
    /// </summary>
    public class AsmResolverTranslator
    {
        private readonly MethodDefinition _method;
        private readonly AsmResolverMetadataMap _metadataMap;
        private readonly MemoryStream _stream = new();
        private readonly BinaryWriter _writer;

        public AsmResolverTranslator(MethodDefinition method, AsmResolverMetadataMap metadataMap)
        {
            _method = method;
            _metadataMap = metadataMap;
            _writer = new BinaryWriter(_stream);
        }

        public byte[] Translate()
        {
            if (_method.CilMethodBody == null) return Array.Empty<byte>();

            foreach (var instr in _method.CilMethodBody.Instructions)
            {
                switch (instr.OpCode.Code)
                {
                    case CilCode.Ldc_I4_M1: Emit(IVMOpcode.LD_I4, -1); break;
                    case CilCode.Ldc_I4_0: Emit(IVMOpcode.LD_I4, 0); break;
                    case CilCode.Ldc_I4_1: Emit(IVMOpcode.LD_I4, 1); break;
                    case CilCode.Ldc_I4_2: Emit(IVMOpcode.LD_I4, 2); break;
                    case CilCode.Ldc_I4_3: Emit(IVMOpcode.LD_I4, 3); break;
                    case CilCode.Ldc_I4_4: Emit(IVMOpcode.LD_I4, 4); break;
                    case CilCode.Ldc_I4_5: Emit(IVMOpcode.LD_I4, 5); break;
                    case CilCode.Ldc_I4_6: Emit(IVMOpcode.LD_I4, 6); break;
                    case CilCode.Ldc_I4_7: Emit(IVMOpcode.LD_I4, 7); break;
                    case CilCode.Ldc_I4_8: Emit(IVMOpcode.LD_I4, 8); break;
                    case CilCode.Ldc_I4_S:
                        Emit(IVMOpcode.LD_I4, (sbyte)(instr.Operand ?? 0));
                        break;
                    case CilCode.Ldc_I4:
                        Emit(IVMOpcode.LD_I4, (int)(instr.Operand ?? 0));
                        break;
                    case CilCode.Ldstr:
                        var str = (string)(instr.Operand ?? "");
                        _writer.Write((byte)IVMOpcode.LD_STR);
                        _writer.Write((byte)str.Length);
                        _writer.Write(Encoding.UTF8.GetBytes(str));
                        break;
                    case CilCode.Call:
                    case CilCode.Callvirt:
                        var methodRef = (IMethodDescriptor?)instr.Operand;
                        if (methodRef != null)
                        {
                            var token = _metadataMap.GetMethodToken(methodRef);
                            Emit(IVMOpcode.CALL, token);
                        }
                        break;
                    case CilCode.Newobj:
                        var ctorRef = (IMethodDescriptor?)instr.Operand;
                        if (ctorRef != null)
                        {
                            var ctorToken = _metadataMap.GetMethodToken(ctorRef);
                            Emit(IVMOpcode.NEWOBJ, ctorToken);
                        }
                        break;
                    case CilCode.Ret: _writer.Write((byte)IVMOpcode.RET); break;
                    case CilCode.Nop: _writer.Write((byte)IVMOpcode.NOP); break;
                    case CilCode.Ldarg_0: Emit(IVMOpcode.LDARG, 0); break;
                    case CilCode.Ldarg_1: Emit(IVMOpcode.LDARG, 1); break;
                    case CilCode.Ldarg_2: Emit(IVMOpcode.LDARG, 2); break;
                    case CilCode.Ldarg_3: Emit(IVMOpcode.LDARG, 3); break;
                    case CilCode.Ldarg_S:
                        Emit(IVMOpcode.LDARG, (int)(byte)(instr.Operand ?? 0));
                        break;
                    case CilCode.Ldarg:
                        Emit(IVMOpcode.LDARG, (int)(instr.Operand ?? 0));
                        break;
                    default:
                        break; // Skip unsupported
                }
            }
            return _stream.ToArray();
        }

        private void Emit(IVMOpcode op, int value)
        {
            _writer.Write((byte)op);
            _writer.Write(value);
        }
    }

    /// <summary>
    /// AsmResolver-compatible metadata mapper
    /// </summary>
    public class AsmResolverMetadataMap
    {
        private readonly Dictionary<string, int> _methodMap = new();
        private readonly Dictionary<string, int> _fieldMap = new();
        private readonly Random _rng = new();

        public AsmResolverMetadataMap(ModuleDefinition module)
        {
            BuildMaps(module);
        }

        private void BuildMaps(ModuleDefinition module)
        {
            var methods = new HashSet<IMethodDescriptor>();
            var fields = new HashSet<IFieldDescriptor>();

            foreach (var type in module.TopLevelTypes)
            {
                foreach (var method in type.Methods)
                {
                    if (method.CilMethodBody == null) continue;
                    foreach (var instr in method.CilMethodBody.Instructions)
                    {
                        switch (instr.Operand)
                        {
                            case IMethodDescriptor m: methods.Add(m); break;
                            case IFieldDescriptor f: fields.Add(f); break;
                        }
                    }
                }
            }

            foreach (var method in methods)
                _methodMap[GetMethodKey(method)] = GetRandomToken();
            foreach (var field in fields)
                _fieldMap[GetFieldKey(field)] = GetRandomToken();
        }

        public int GetMethodToken(IMethodDescriptor method) =>
            _methodMap.TryGetValue(GetMethodKey(method), out var token)
                ? token : throw new KeyNotFoundException($"Method not in map: {method.FullName}");

        public int GetFieldToken(IFieldDescriptor field) =>
            _fieldMap.TryGetValue(GetFieldKey(field), out var token)
                ? token : throw new KeyNotFoundException($"Field not in map: {field.FullName}");

        private string GetMethodKey(IMethodDescriptor m)
        {
            var paramTypes = m.Signature?.ParameterTypes.Select(p => GetTypeFullName(p)) ?? Enumerable.Empty<string>();
            return $"{m.DeclaringType?.FullName ?? "Unknown"}::{m.Name}({string.Join(",", paramTypes)})";
        }

        private string GetFieldKey(IFieldDescriptor f)
        {
            var typeName = GetTypeFullName(f.Signature?.FieldType);
            return $"{f.DeclaringType?.FullName ?? "Unknown"}::{f.Name}({typeName})";
        }

        private string GetTypeFullName(TypeSignature? type)
        {
            if (type == null) return "null";
            
            // ✅ Simplified approach - use Name property or FullName
            if (type is CorLibTypeSignature corlib)
            {
                // Use the Name property which gives us the simple type name
                return corlib.Name?.ToString() switch
                {
                    "Void" => "void",
                    "Boolean" => "bool",
                    "Char" => "char",
                    "SByte" => "sbyte",
                    "Byte" => "byte",
                    "Int16" => "short",
                    "UInt16" => "ushort",
                    "Int32" => "int",
                    "UInt32" => "uint",
                    "Int64" => "long",
                    "UInt64" => "ulong",
                    "Single" => "float",
                    "Double" => "double",
                    "String" => "string",
                    "Object" => "object",
                    _ => corlib.FullName
                };
            }
            
            if (type is TypeDefOrRefSignature typeRef)
                return typeRef.FullName;
            
            return type.FullName;
        }

        private int GetRandomToken() => _rng.Next(0x10000000, int.MaxValue);

        public byte[] GetEncryptedMetadataBlob(byte[] key)
        {
            var model = new
            {
                methods = _methodMap,
                fields = _fieldMap
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var compressed = Compress(Encoding.UTF8.GetBytes(json));
            return PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(compressed, key);
        }

        private byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var compressor = new DeflateStream(output, CompressionLevel.Optimal))
            {
                compressor.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }
}
#endif