using System;
using System.Windows.Forms; // ✅ Critical for STAThread, Application, MessageBox
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace PhantomExe.WinForms
{
    internal static class Program
    {
        [STAThread] // ✅ Now recognized
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => 
                MessageBox.Show($"Startup error:\n{e.Exception.Message}", "Fatal");

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical failure:\n{ex}", "Fatal");
            }
        }
    }
}