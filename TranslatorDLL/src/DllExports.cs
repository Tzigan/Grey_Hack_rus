using System;
using System.IO;
using RGiesecke.DllExport;

namespace GreyHackTranslator
{
    public static class DllExports
    {
        [DllExport("Init", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static void InitExport()
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "greyhack_export_debug.log");

                File.AppendAllText(logPath, $"[{DateTime.Now}] Init функция вызвана\n");
                TranslatorPlugin.Init();

                File.AppendAllText(logPath, $"[{DateTime.Now}] Init вызов завершен\n");
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "greyhack_export_error.log"),
                        $"[{DateTime.Now}] Ошибка: {ex.Message}\n{ex.StackTrace}\n");
                }
                catch { }
            }
        }
    }
}