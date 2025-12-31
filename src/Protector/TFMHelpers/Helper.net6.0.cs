#if NET6_0_OR_GREATER
using AsmResolver.DotNet.Code.Cil; // ✅ AsmResolver for modern .NET
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace PhantomExe.Protector.TFMHelpers
{
    internal static class TFMHelper
    {
        public static void SimplifyMacros(CilMethodBody body)
        {
            // ✅ AsmResolver CilMethodBody doesn't need SimplifyMacros
            // AsmResolver handles macro expansion automatically during write
            // This method exists for API compatibility with dnlib version
        }

        public static MethodInfo GetKeyBytesMethod()
        {
            var dm = new DynamicMethod("GetKeyBytes", 
                typeof(byte[]), new[] { typeof(long), typeof(long) });
            
            var il = dm.GetILGenerator();
            
            // byte[] key = new byte[16];
            il.Emit(OpCodes.Ldc_I4, 16);
            il.Emit(OpCodes.Newarr, typeof(byte));
            il.Emit(OpCodes.Dup); // [key, key]
            
            // key[0..7] = BitConverter.GetBytes(keyHi)
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(BitConverter).GetMethod("GetBytes", new[] { typeof(long) })!);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_8);
            il.Emit(OpCodes.Call, typeof(Array).GetMethod("Copy", new Type[] { 
                typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })!);
            
            // key[8..15] = BitConverter.GetBytes(keyLo)
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_8);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(BitConverter).GetMethod("GetBytes", new[] { typeof(long) })!);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_8);
            il.Emit(OpCodes.Call, typeof(Array).GetMethod("Copy", new Type[] { 
                typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })!);
            
            il.Emit(OpCodes.Ret);
            return dm;
        }
    }
}
#endif