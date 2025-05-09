using Grey_Hack_rus;
using System;
using System.Runtime.InteropServices;

namespace GreyHackTranslator
{
    public static class DllExports
    {
        // Важно: пространство имен для DllExport может отличаться в зависимости от версии
        // Попробуйте один из этих вариантов, если компилятор выдает ошибку

        // Вариант 1
        [DllExport("Init", CallingConvention = CallingConvention.Cdecl)]
        public static void InitExport()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(desktopPath, "greyhack_dllexport_debug.log"),
                    $"[{DateTime.Now}] InitExport вызван через DllExport\n");

                TranslatorPlugin.Init();
            }
            catch (Exception ex)
            {
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(desktopPath, "greyhack_dllexport_debug.log"),
                        $"[{DateTime.Now}] Ошибка в InitExport: {ex.Message}\n{ex.StackTrace}\n");
                }
                catch { }
            }
        }
    }
}