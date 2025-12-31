using PhantomExe.Core.Crypto;
using PhantomExe.Core.Security;
using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace PhantomExe.Core.VM
{
    public sealed class IVMContext : IDisposable
    {
        private readonly byte[] _encryptedBytecode;
        private readonly byte[] _decryptionKey;
        private readonly Dictionary<int, MethodInfo> _methodCache = new();
        private readonly Stack<object?> _stack = new(32);
        private bool _disposed;

        public IVMContext(string methodId, byte[] encryptedBytecode, byte[] key)
        {
            _encryptedBytecode = encryptedBytecode;
            _decryptionKey = key;
            
            // Initialize method resolver
            InitializeMethodResolver();
        }

        private void InitializeMethodResolver()
        {
            try
            {
                // Load encrypted metadata from resources
                var asm = Assembly.GetCallingAssembly();
                using var stream = asm.GetManifestResourceStream("ivm.metadata");
                if (stream == null)
                    return;
                    
                var encryptedMetadata = new byte[stream.Length];
                stream.Read(encryptedMetadata, 0, encryptedMetadata.Length);
                
                // Initialize the resolver
                MethodResolver.Initialize(encryptedMetadata, _decryptionKey);
            }
            catch
            {
                // Silently fail
            }
        }

        public object? Execute(object?[]? args)
        {
            TamperGuard.CheckDebugger();
            var vmCode = AesGcmUtil.Decrypt(_encryptedBytecode, _decryptionKey);
            return Interpret(vmCode, args ?? Array.Empty<object?>());
        }

        private object? Interpret(byte[] code, object?[] args)
        {
            int ip = 0;
            while (ip < code.Length)
            {
                var op = (IVMOpcode)code[ip++];
                switch (op)
                {
                    case IVMOpcode.LD_I4:
                        _stack.Push(BitConverter.ToInt32(code, ip)); 
                        ip += 4; 
                        break;
                        
                    case IVMOpcode.LD_STR:
                        var len = code[ip++];
                        var str = Encoding.UTF8.GetString(code, ip, len); 
                        ip += len;
                        _stack.Push(str); 
                        break;
                        
                    case IVMOpcode.LDARG:
                        var idx = code[ip++]; 
                        _stack.Push(args[idx]); 
                        break;
                        
                    case IVMOpcode.CALL:
                        var token = BitConverter.ToInt32(code, ip); 
                        ip += 4;
                        var method = ResolveMethod(token);
                        var paramCount = method.GetParameters().Length;
                        var callArgs = new object?[paramCount];
                        for (int i = 0; i < paramCount; i++)
                            callArgs[paramCount - 1 - i] = _stack.Pop();
                        var result = method.Invoke(null, callArgs);
                        _stack.Push(result);
                        break;
                        
                    case IVMOpcode.NEWOBJ:
                        var ctorToken = BitConverter.ToInt32(code, ip);
                        ip += 4;
                        var ctor = ResolveMethod(ctorToken);
                        var ctorParamCount = ctor.GetParameters().Length;
                        var ctorArgs = new object?[ctorParamCount];
                        for (int i = 0; i < ctorParamCount; i++)
                            ctorArgs[ctorParamCount - 1 - i] = _stack.Pop();
                        var instance = ctor.Invoke(null, ctorArgs);
                        _stack.Push(instance);
                        break;
                        
                    case IVMOpcode.RET:
                        return _stack.Pop();
                        
                    case IVMOpcode.TRAP:
                        TamperGuard.TriggerAntiDebug(); 
                        break;
                        
                    case IVMOpcode.NOP: 
                        break;
                        
                    default:
                        throw new InvalidProgramException($"Invalid opcode: 0x{(byte)op:X2}");
                }
            }
            throw new InvalidOperationException("Missing RET");
        }

        private MethodInfo ResolveMethod(int token)
        {
            if (!_methodCache.TryGetValue(token, out var method))
            {
                method = MethodResolver.ResolveMethod(token);
                _methodCache[token] = method;
            }
            return method;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Array.Clear(_decryptionKey, 0, _decryptionKey.Length);
            _methodCache.Clear();
            _stack.Clear();
            _disposed = true;
        }
    }
}