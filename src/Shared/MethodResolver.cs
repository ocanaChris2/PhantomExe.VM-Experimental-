using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace PhantomExe.Core.VM
{
    internal static class MethodResolver
    {
        private static readonly object _lock = new();
        private static MetadataCache? _cache;
        
        public static bool IsInitialized => _cache != null;

        public static void Initialize(byte[] encryptedMetadata, byte[] key)
        {
            lock (_lock)
            {
                if (_cache != null) return;

                // Decrypt and decompress metadata
                var decrypted = PhantomExe.Core.Crypto.AesGcmUtil.DecryptMetadata(encryptedMetadata, key);
                var decompressed = Decompress(decrypted);
                var json = Encoding.UTF8.GetString(decompressed);

                var model = JsonSerializer.Deserialize<MetadataModel>(json)
                    ?? throw new InvalidOperationException("Invalid metadata format");

                _cache = new MetadataCache(model);
            }
        }

        public static MethodInfo ResolveMethod(int token)
        {
            EnsureInitialized();
            if (!_cache!.Methods.TryGetValue(token, out var descriptor))
                throw new KeyNotFoundException($"Method token {token:X8} not found");

            var type = ResolveType(descriptor.DeclaringType);
            var parameterTypes = ResolveParameterTypes(descriptor.Parameters);
            
            var method = type.GetMethod(
                descriptor.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance,
                binder: null,
                types: parameterTypes,
                modifiers: null)
                ?? throw new MissingMethodException($"Method '{descriptor.Name}' not found in {descriptor.DeclaringType}");

            return method;
        }

        public static FieldInfo ResolveField(int token)
        {
            EnsureInitialized();
            if (!_cache!.Fields.TryGetValue(token, out var descriptor))
                throw new KeyNotFoundException($"Field token {token:X8} not found");

            var type = ResolveType(descriptor.DeclaringType);
            var field = type.GetField(
                descriptor.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?? throw new MissingFieldException($"Field '{descriptor.Name}' not found in {descriptor.DeclaringType}");

            return field;
        }

        private static Type[] ResolveParameterTypes(List<string>? paramNames)
        {
            if (paramNames == null || paramNames.Count == 0)
                return Type.EmptyTypes;

            var types = new Type[paramNames.Count];
            for (int i = 0; i < paramNames.Count; i++)
            {
                types[i] = ResolveType(paramNames[i]);
            }
            return types;
        }

        public static Type ResolveType(string fullName)
        {
            // Normalize type name (remove assembly version/public key)
            var cleanName = fullName.Split(',')[0].Trim();
            
            // Built-in types mapping
            return cleanName switch
            {
                "System.Void" => typeof(void),
                "System.Boolean" => typeof(bool),
                "System.Char" => typeof(char),
                "System.SByte" => typeof(sbyte),
                "System.Byte" => typeof(byte),
                "System.Int16" => typeof(short),
                "System.UInt16" => typeof(ushort),
                "System.Int32" => typeof(int),
                "System.UInt32" => typeof(uint),
                "System.Int64" => typeof(long),
                "System.UInt64" => typeof(ulong),
                "System.Single" => typeof(float),
                "System.Double" => typeof(double),
                "System.String" => typeof(string),
                "System.Object" => typeof(object),
                _ => ResolveCustomType(cleanName)
            };
        }

        private static Type ResolveCustomType(string fullName)
        {
            // Try current assembly first
            var type = Type.GetType(fullName);
            if (type != null) return type;

            // Search all loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(fullName);
                if (type != null) return type;
            }

            throw new TypeLoadException($"Type '{fullName}' not found");
        }

        private static void EnsureInitialized()
        {
            if (_cache == null)
                throw new InvalidOperationException("Metadata not initialized. Call Initialize() first.");
        }

        private static byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var decompressor = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            decompressor.CopyTo(output);
            return output.ToArray();
        }

        // JSON models
        private class MetadataModel
        {
            public Dictionary<int, MethodDescriptor> Methods { get; set; } = new();
            public Dictionary<int, FieldDescriptor> Fields { get; set; } = new();
        }

        private class MethodDescriptor
        {
            public string DeclaringType { get; set; } = "";
            public string Name { get; set; } = "";
            public List<string>? Parameters { get; set; }
        }

        private class FieldDescriptor
        {
            public string DeclaringType { get; set; } = "";
            public string Name { get; set; } = "";
        }

        private class MetadataCache
        {
            public Dictionary<int, MethodDescriptor> Methods { get; }
            public Dictionary<int, FieldDescriptor> Fields { get; }

            public MetadataCache(MetadataModel model)
            {
                Methods = model.Methods;
                Fields = model.Fields;
            }
        }
    }
}