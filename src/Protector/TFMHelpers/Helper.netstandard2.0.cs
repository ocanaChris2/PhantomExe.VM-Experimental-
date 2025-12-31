// src/Protector/TFMHelpers/Helper.netstandard2.0.cs
#if NETSTANDARD2_0
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Reflection;
using EmitOpCodes = System.Reflection.Emit.OpCodes; // ✅ Alias to resolve ambiguity

namespace PhantomExe.Protector.TFMHelpers
{
    internal static class TFMHelper
    {
        // ParameterList required for dnlib 3.6.0 netstandard2.0
        internal sealed class ParameterList : System.Collections.Generic.IList<Parameter>
        {
            private readonly System.Collections.Generic.List<Parameter> _list = new();
            public Parameter this[int index] { get => _list[index]; set => _list[index] = value; }
            public int Count => _list.Count;
            public bool IsReadOnly => false;
            public void Add(Parameter item) => _list.Add(item);
            public void Clear() => _list.Clear();
            public bool Contains(Parameter item) => _list.Contains(item);
            public void CopyTo(Parameter[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
            public System.Collections.Generic.IEnumerator<Parameter> GetEnumerator() => _list.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _list.GetEnumerator();
            public int IndexOf(Parameter item) => _list.IndexOf(item);
            public void Insert(int index, Parameter item) => _list.Insert(index, item);
            public bool Remove(Parameter item) => _list.Remove(item);
            public void RemoveAt(int index) => _list.RemoveAt(index);
        }

        public static void SimplifyMacros(CilBody body)
        {
            body.SimplifyMacros(new ParameterList());
        }

        public static MethodInfo GetKeyBytesMethod()
        {
            var dm = new System.Reflection.Emit.DynamicMethod("GetKeyBytes",
                typeof(byte[]), new[] { typeof(long), typeof(long) });

            var il = dm.GetILGenerator();

            // byte[] key = new byte[16];
            il.Emit(EmitOpCodes.Ldc_I4, 16);
            il.Emit(EmitOpCodes.Newarr, typeof(byte));
            il.Emit(EmitOpCodes.Dup); // [key, key]

            // key[0..7] = BitConverter.GetBytes(keyHi)
            il.Emit(EmitOpCodes.Ldc_I4_0);
            il.Emit(EmitOpCodes.Ldarg_0);
            il.Emit(EmitOpCodes.Call, typeof(BitConverter).GetMethod("GetBytes", new[] { typeof(long) })!);
            il.Emit(EmitOpCodes.Ldc_I4_0);
            il.Emit(EmitOpCodes.Ldc_I4_8);
            il.Emit(EmitOpCodes.Call, typeof(Array).GetMethod("Copy", new Type[] {
                typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })!);

            // key[8..15] = BitConverter.GetBytes(keyLo)
            il.Emit(EmitOpCodes.Dup);
            il.Emit(EmitOpCodes.Ldc_I4_8);
            il.Emit(EmitOpCodes.Ldarg_1);
            il.Emit(EmitOpCodes.Call, typeof(BitConverter).GetMethod("GetBytes", new[] { typeof(long) })!);
            il.Emit(EmitOpCodes.Ldc_I4_0);
            il.Emit(EmitOpCodes.Ldc_I4_8);
            il.Emit(EmitOpCodes.Call, typeof(Array).GetMethod("Copy", new Type[] {
                typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })!);

            il.Emit(EmitOpCodes.Ret);
            return dm;
        }
    }
}
#endif