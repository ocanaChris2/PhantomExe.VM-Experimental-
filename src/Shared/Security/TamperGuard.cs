using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// ✅ Add missing using
using System.Security.Cryptography;


namespace PhantomExe.Core.Security
{
    public static class TamperGuard
    {
        private static readonly byte[] _entropy = new byte[32];
        static TamperGuard()
        {
#if NET6_0_OR_GREATER
            RandomNumberGenerator.Fill(_entropy); // .NET 6.0+
#else
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(_entropy);
#endif
        }

        public static void CheckDebugger()
        {
            // ✅ Fix: Environment.ProcessPath → fallback for netstandard2.0
            string processPath = 
#if NET6_0_OR_GREATER
                Environment.ProcessPath;
#else
                Environment.GetCommandLineArgs().Length > 0 ? 
                    Environment.GetCommandLineArgs()[0] : "";
#endif

            if (Debugger.IsAttached ||
                processPath?.IndexOf("dnspy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                IsDebuggerPresentWin32() ||
                TimingCheck())
            {
                TriggerAntiDebug();
            }
        }

        [DllImport("kernel32.dll")] private static extern bool IsDebuggerPresent();
        private static bool IsDebuggerPresentWin32() => IsDebuggerPresent();

        private static bool TimingCheck()
        {
            var sw = Stopwatch.StartNew();
            Thread.Sleep(1);
            return sw.ElapsedMilliseconds > 10;
        }

        public static void TriggerAntiDebug()
        {
            var action = _entropy[0] % 3;
            switch (action)
            {
                case 0: throw new StackOverflowException();
                case 1: while (true) Thread.SpinWait(1000);  // ✅ Add break
                case 2: Environment.Exit(-1); break; // ✅ Add break
            }
        }
    }
}