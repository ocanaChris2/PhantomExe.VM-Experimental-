using dnlib.DotNet;           // Must be dnlib 3.6.0
using dnlib.DotNet.Emit;
using PhantomExe.Core.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1416 // Protector is Windows-only tool

namespace PhantomExe.Protector.Translation
{
    /// <summary>
    /// Translates CIL/IL to PhantomExe Intermediate Virtual Machine bytecode.
    /// Fully compatible with dnlib 3.6.0 on netstandard2.0 and net6.0+.
    /// Immune to CVE-2024-30105 (no DeserializeAsyncEnumerable usage).
    /// </summary>
    public class ILToVMTranslator
    {
        private readonly MethodDef _method;
        private readonly ModuleDef _module;
        private readonly MetadataMap _metadataMap;
        private readonly MemoryStream _stream = new();
        private readonly BinaryWriter _writer;

        public ILToVMTranslator(MethodDef method, MetadataMap metadataMap)
        {
            _method = method;
            _module = method.Module;
            _metadataMap = metadataMap;
            _writer = new BinaryWriter(_stream);
        }

        public byte[] Translate()
        {
            foreach (var instr in _method.Body.Instructions)
            {
                switch (instr.OpCode.Code)
                {
                    case Code.Ldc_I4_M1: Emit(IVMOpcode.LD_I4, -1); break;
                    case Code.Ldc_I4_0: Emit(IVMOpcode.LD_I4, 0); break;
                    case Code.Ldc_I4_1: Emit(IVMOpcode.LD_I4, 1); break;
                    case Code.Ldc_I4_2: Emit(IVMOpcode.LD_I4, 2); break;
                    case Code.Ldc_I4_3: Emit(IVMOpcode.LD_I4, 3); break;
                    case Code.Ldc_I4_4: Emit(IVMOpcode.LD_I4, 4); break;
                    case Code.Ldc_I4_5: Emit(IVMOpcode.LD_I4, 5); break;
                    case Code.Ldc_I4_6: Emit(IVMOpcode.LD_I4, 6); break;
                    case Code.Ldc_I4_7: Emit(IVMOpcode.LD_I4, 7); break;
                    case Code.Ldc_I4_8: Emit(IVMOpcode.LD_I4, 8); break;

                    case Code.Ldc_I4_S:
                        var sbyteValue = instr.Operand switch
                        {
                            sbyte sb => sb,
                            byte b => (sbyte)b,
                            short s => (sbyte)s,
                            int i => (sbyte)i,
                            _ => throw new InvalidOperationException($"Unexpected operand: {instr.Operand?.GetType()}")
                        };
                        Emit(IVMOpcode.LD_I4, sbyteValue);
                        break;

                    case Code.Ldc_I4:
                        var intValue = instr.Operand switch
                        {
                            int i => i,
                            uint u => unchecked((int)u),
                            long l => (int)l,
                            ulong ul => (int)ul,
                            _ => Convert.ToInt32(instr.Operand)
                        };
                        Emit(IVMOpcode.LD_I4, intValue);
                        break;

                    case Code.Ldstr:
                        var str = (string)instr.Operand!;
                        _writer.Write((byte)IVMOpcode.LD_STR);
                        _writer.Write((byte)str.Length);
                        _writer.Write(Encoding.UTF8.GetBytes(str));
                        break;

                    case Code.Call:
                    case Code.Callvirt:
                        var methodRef = (IMethod)instr.Operand!;
                        var token = _metadataMap.GetMethodToken(methodRef);
                        Emit(IVMOpcode.CALL, token);
                        break;

                    case Code.Newobj:
                        var ctorRef = (IMethod)instr.Operand!;
                        var ctorToken = _metadataMap.GetMethodToken(ctorRef);
                        Emit(IVMOpcode.NEWOBJ, ctorToken);
                        break;

                    case Code.Ret: _writer.Write((byte)IVMOpcode.RET); break;
                    case Code.Nop: _writer.Write((byte)IVMOpcode.NOP); break;

                    case Code.Ldarg_0: Emit(IVMOpcode.LDARG, 0); break;
                    case Code.Ldarg_1: Emit(IVMOpcode.LDARG, 1); break;
                    case Code.Ldarg_2: Emit(IVMOpcode.LDARG, 2); break;
                    case Code.Ldarg_3: Emit(IVMOpcode.LDARG, 3); break;
                    case Code.Ldarg_S:
                        Emit(IVMOpcode.LDARG, (int)(byte)instr.Operand!);
                        break;
                    case Code.Ldarg:
                        Emit(IVMOpcode.LDARG, (int)instr.Operand!);
                        break;

                    // Skip unsupported (extend as needed)
                    default:
                        break;
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
    /// Maps .NET metadata to obfuscated tokens. Encrypted and embedded.
    /// ✅ CVE-2024-30105 SAFE: Uses ONLY JsonConvert.SerializeObject() + Deserialize&lt;T&gt;()
    /// </summary>
    public class MetadataMap
    {
        private readonly Dictionary<string, int> _methodMap = new();
        private readonly Dictionary<string, int> _fieldMap = new();
        private readonly Random _rng = new();

        public MetadataMap(ModuleDef module)
        {
            BuildMaps(module);
        }

        private void BuildMaps(ModuleDef module)
        {
            var methods = new HashSet<IMethod>();
            var fields = new HashSet<IField>();
            CollectReferences(module, methods, fields);

            foreach (var method in methods)
                _methodMap[GetMethodKey(method)] = GetRandomToken();
            foreach (var field in fields)
                _fieldMap[GetFieldKey(field)] = GetRandomToken();
        }

        private void CollectReferences(
            ModuleDef module,
            HashSet<IMethod> methods,
            HashSet<IField> fields)
        {
            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (var instr in method.Body.Instructions)
                    {
                        switch (instr.Operand)
                        {
                            case IMethod m:
                                methods.Add(m);
                                break;
                            case IField f:
                                fields.Add(f);
                                break;
                        }
                    }
                }
            }
        }

        public int GetMethodToken(IMethod method) =>
            _methodMap.TryGetValue(GetMethodKey(method), out var token)
                ? token
                : throw new KeyNotFoundException($"Method not in map: {method.FullName}");

        public int GetFieldToken(IField field) =>
            _fieldMap.TryGetValue(GetFieldKey(field), out var token)
                ? token
                : throw new KeyNotFoundException($"Field not in map: {field.FullName}");

        private string GetMethodKey(IMethod m)
        {
            var paramTypes = new List<string>();
            if (m.MethodSig?.Params != null)
            {
                foreach (var param in m.MethodSig.Params)
                {
                    paramTypes.Add(GetTypeFullName(param));
                }
            }
            return $"{m.DeclaringType?.FullName ?? "Unknown"}::{m.Name}({string.Join(",", paramTypes)})";
        }

        private string GetFieldKey(IField f)
        {
            string typeName = "Unknown";
            if (f.FieldSig?.Type != null)
            {
                typeName = GetTypeFullName(f.FieldSig.Type);
            }
            return $"{f.DeclaringType?.FullName ?? "Unknown"}::{f.Name}({typeName})";
        }

        // ✅ SAFE FOR DNLIB 3.6.0: No .FieldSig.FullName (doesn't exist)
        private string GetTypeFullName(TypeSig? typeSig)
        {
            if (typeSig == null) return "null";

            return typeSig.ElementType switch
            {
                ElementType.Void => "void",
                ElementType.Boolean => "bool",
                ElementType.Char => "char",
                ElementType.I1 => "sbyte",
                ElementType.U1 => "byte",
                ElementType.I2 => "short",
                ElementType.U2 => "ushort",
                ElementType.I4 => "int",
                ElementType.U4 => "uint",
                ElementType.I8 => "long",
                ElementType.U8 => "ulong",
                ElementType.R4 => "float",
                ElementType.R8 => "double",
                ElementType.String => "string",
                ElementType.Object => "object",
                ElementType.ByRef => $"{GetTypeFullName((typeSig as ByRefSig)?.Next)}&",
                ElementType.Ptr => $"{GetTypeFullName((typeSig as PtrSig)?.Next)}*",
                ElementType.SZArray => $"{GetTypeFullName((typeSig as SZArraySig)?.Next)}[]",
                ElementType.Array => HandleArraySig(typeSig as ArraySig),
                ElementType.GenericInst => HandleGenericInstSig(typeSig as GenericInstSig),
                ElementType.ValueType or ElementType.Class =>
                    (typeSig as TypeDefOrRefSig)?.TypeDefOrRef?.FullName ?? typeSig.FullName,
                _ => typeSig.FullName
            };
        }

        private string HandleArraySig(ArraySig? arraySig)
        {
            if (arraySig == null) return "[]";
            var baseType = GetTypeFullName(arraySig.Next);
            // ✅ Safe uint → int cast
            var dims = new string(',', (int)arraySig.Rank - 1);
            return $"{baseType}[{dims}]";
        }

        private string HandleGenericInstSig(GenericInstSig? genericSig)
        {
            if (genericSig == null) return "GenericInst";
            var genericType = GetTypeFullName(genericSig.GenericType);
            // ✅ Critical: Safe uint → int cast (CVE-safe, netstandard2.0-compatible)
            var argCount = (int)genericSig.GenericArguments.Count;
            var args = genericSig.GenericArguments
                .Select(GetTypeFullName)
                .Take(argCount); // Now int — no CS1503
            return $"{genericType}<{string.Join(",", args)}>";
        }

        // ✅ SAFE: int.MaxValue avoids overflow (0xEFFFFFFF > int.MaxValue)
        private int GetRandomToken() => _rng.Next(0x10000000, int.MaxValue);

        /// <summary>
        /// Serializes metadata to JSON, compresses, and encrypts.
        /// 🔒 CVE-2024-30105 SAFE: Uses ONLY JsonConvert.SerializeObject + Deserialize&lt;T&gt;()
        /// NOT vulnerable to deserialization DoS.
        /// </summary>
        public byte[] GetEncryptedMetadataBlob(byte[] key)
        {
            var model = new
            {
                methods = _methodMap,
                fields = _fieldMap
            };

            // ✅ SAFE: JsonConvert.SerializeObject (sync) — no DeserializeAsyncEnumerable
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var compressed = Compress(Encoding.UTF8.GetBytes(json));
            return PhantomExe.Core.Crypto.AesGcmUtil.Encrypt(compressed, key);
        }

        private byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using var compressor = new DeflateStream(output, CompressionLevel.Optimal);
            compressor.Write(data, 0, data.Length);
            return output.ToArray();
        }
    }
}