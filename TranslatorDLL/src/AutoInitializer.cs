using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GreyHackTranslator
{
    // Класс, который гарантированно загрузится при загрузке сборки
    public static class AutoInitializer
    {
        // Для .NET 4.0+
        [ModuleInitializer]
        internal static void Initialize()
        {
            SafeInitialize();
        }

        // Статический конструктор (если ModuleInitializer не сработает)
        static AutoInitializer()
        {
            SafeInitialize();
        }

        // Безопасная инициализация с обработкой всех исключений
        public static void SafeInitialize()
        {
            try
            {
                // Пути для логов в разных местах
                string[] logPaths = new[] {
                    // Рабочий стол
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "greyhack_dll_init.log"),
                    // Папка с игрой
                    Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "greyhack_dll_init.log"),
                    // Текущая директория
                    "greyhack_dll_init.log",
                    // Папка с DLL
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "greyhack_dll_init.log")
                };

                // Пробуем записать в каждый из путей
                foreach (var logPath in logPaths)
                {
                    try
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now}] AutoInitializer.SafeInitialize вызван\n");
                    }
                    catch { }
                }

                // Пробуем инициализировать TranslatorPlugin
                TranslatorPlugin.Init();
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(
                        Path.Combine(Path.GetTempPath(), "greyhack_error.log"),
                        $"[{DateTime.Now}] Ошибка инициализации: {ex.Message}\n{ex.StackTrace}");
                }
                catch { }
            }
        }
    }
}