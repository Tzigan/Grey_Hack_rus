using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GreyHackTranslator
{
    public class TranslatorPlugin
    {
        public static Dictionary<string, string> translationDictionary = new Dictionary<string, string>();
        private static bool isInitialized = false;
        private static string translationFilePath = "russian_translation.txt";
        private static string logPath = "translator_log.txt";
        private static readonly string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private const string DEBUG_PREFIX = "[GH_RUS] ";

        // Статический конструктор - вызывается автоматически при загрузке класса
        static TranslatorPlugin()
        {
            try
            {
                // Запускаем инициализацию при загрузке DLL
                EmergencyLog("DLL загружена, автоматический запуск инициализации из статического конструктора");
                Init();

                // Также проверяем наличие файла-флага
                string flagFile = "init_translator.flag";
                if (File.Exists(flagFile))
                {
                    EmergencyLog($"Обнаружен файл-флаг {Path.GetFullPath(flagFile)}, запуск дополнительной инициализации...");
                    // Дополнительная инициализация при необходимости
                }
            }
            catch (Exception ex)
            {
                EmergencyLog($"Ошибка в статическом конструкторе: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Добавляем метод для экстренного логирования на рабочий стол
        public static void EmergencyLog(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(desktopPath, "greyhack_injector_debug.log"),
                    $"[{DateTime.Now}] {message}\n");
            }
            catch (Exception ex)
            {
                // Если даже это не работает, попробуем создать файл в папке с игрой
                try
                {
                    File.AppendAllText("emergency_debug.log",
                        $"[{DateTime.Now}] FAILED TO WRITE TO DESKTOP: {ex.Message}\n" +
                        $"[{DateTime.Now}] ORIGINAL MESSAGE: {message}\n");
                }
                catch { }
            }
        }

        // Точка входа для инжектора с публичным модификатором и атрибутом
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        // Публичный метод инициализации для упрощения вызова
        public static void Init()
        {
            EmergencyLog($"DLL загружена в процесс в {DateTime.Now}");
            InitializeTranslator();
        }

        // Основной метод инициализации
        private static void InitializeTranslator()
        {
            if (isInitialized) return;

            try
            {
                EmergencyLog("Начало инициализации переводчика");
                Log("Инициализация переводчика...");

                // Информация о среде
                EmergencyLog($"Текущая директория: {Directory.GetCurrentDirectory()}");
                EmergencyLog($"Путь к сборке: {Assembly.GetExecutingAssembly().Location}");

                // Получаем информацию о версии Harmony
                string harmonyVersion = typeof(Harmony).Assembly.GetName().Version.ToString();
                EmergencyLog($"Версия Harmony: {harmonyVersion}");
                Log($"Версия Harmony: {harmonyVersion}");

                // Выводим текущую директорию и путь к файлу перевода
                string currentDir = Directory.GetCurrentDirectory();
                EmergencyLog($"Текущая директория: {currentDir}");
                EmergencyLog($"Путь к файлу перевода: {Path.GetFullPath(translationFilePath)}");

                try
                {
                    // Попытка использовать Debug.Log
                    Debug.Log($"{DEBUG_PREFIX}DLL загружена и инициализируется");
                    EmergencyLog("Debug.Log успешно вызван");
                }
                catch (Exception ex)
                {
                    EmergencyLog($"Ошибка при вызове Debug.Log: {ex.Message}");
                }

                // Загружаем переводы из имеющегося файла переводов
                LoadTranslations();

                // Информация о загруженных сборках
                EmergencyLog("Загруженные сборки:");
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    EmergencyLog($"  - {assembly.GetName().Name} v{assembly.GetName().Version}");
                }

                // Создаем экземпляр Harmony для патчинга
                EmergencyLog("Создаем экземпляр Harmony...");
                var harmony = new Harmony("com.tzigan.greyhack.rus");

                // Применяем все патчи в этой сборке
                EmergencyLog("Применяем патчи...");
                try
                {
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                    EmergencyLog("Патчи успешно применены");
                }
                catch (Exception ex)
                {
                    EmergencyLog($"Ошибка при применении патчей: {ex.Message}\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        EmergencyLog($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                    }
                }

                // Анализ UI для отладки
                try
                {
                    EmergencyLog("Поиск UI компонентов...");
                    var allTexts = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
                    EmergencyLog($"Найдено {allTexts.Length} текстовых компонентов");

                    int count = 0;
                    foreach (var text in allTexts)
                    {
                        if (count++ < 10)
                        {
                            EmergencyLog($"Text[{count}]: '{text.text}' на объекте '{text.gameObject.name}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    EmergencyLog($"Ошибка при поиске UI компонентов: {ex.Message}");
                }

                EmergencyLog("Переводчик успешно инициализирован");
                Log("Переводчик успешно инициализирован");
                isInitialized = true;
            }
            catch (Exception ex)
            {
                EmergencyLog($"Критическая ошибка инициализации: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    EmergencyLog($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
                Log($"Ошибка инициализации: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Загрузка переводов из файла
        private static void LoadTranslations()
        {
            try
            {
                if (File.Exists(translationFilePath))
                {
                    EmergencyLog($"Загрузка переводов из {translationFilePath}...");
                    string[] lines = File.ReadAllLines(translationFilePath);
                    int count = 0;

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || !line.Contains("=")) continue;

                        string[] parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            translationDictionary[key] = value;
                            count++;

                            // Выводим первые 5 переводов для отладки
                            if (count <= 5)
                            {
                                EmergencyLog($"Загружен перевод: '{key}' -> '{value}'");
                            }
                        }
                    }

                    EmergencyLog($"Загружено {count} строк перевода");
                    Log($"Загружено {count} строк перевода из {translationFilePath}");
                }
                else
                {
                    EmergencyLog($"Файл перевода не найден: {Path.GetFullPath(translationFilePath)}");
                    Log($"Файл перевода не найден: {Path.GetFullPath(translationFilePath)}");

                    // Создаем пустой файл перевода
                    File.WriteAllText(translationFilePath, "# Формат: оригинальный текст=переведенный текст\n");
                    EmergencyLog($"Создан пустой файл перевода");
                    Log("Создан пустой файл перевода");
                }
            }
            catch (Exception ex)
            {
                EmergencyLog($"Ошибка загрузки переводов: {ex.Message}\n{ex.StackTrace}");
                Log($"Ошибка загрузки переводов: {ex.Message}");
            }
        }

        // Метод для перевода текста - изменяем для наглядной отладки
        public static string TranslateText(string original)
        {
            if (string.IsNullOrEmpty(original)) return original;

            // Добавляем префикс для визуального отслеживания работы патчей
            string modifiedText = "[RUS] " + original;

            // Если есть перевод в словаре, используем его
            if (translationDictionary.TryGetValue(original, out string translation))
            {
                modifiedText = translation;

                // Логируем успешный перевод
                int hashCode = original.GetHashCode();
                if (Math.Abs(hashCode % 100) == 0) // Ограничиваем количество логов
                {
                    EmergencyLog($"Перевод: '{original}' -> '{translation}'");
                }
                return modifiedText;
            }

            // Записываем непереведенные строки в отдельный файл
            try
            {
                // Не логируем слишком короткие строки (скорее всего одиночные символы)
                if (original.Length > 2)
                {
                    File.AppendAllText("untranslated.txt", original + "\n");

                    // Выводим информацию о непереведенной строке не слишком часто
                    int hashCode = original.GetHashCode();
                    if (Math.Abs(hashCode % 50) == 0)
                    {
                        EmergencyLog($"Не найден перевод: '{original}'");
                    }
                }
            }
            catch (Exception ex)
            {
                EmergencyLog($"Ошибка записи непереведенной строки: {ex.Message}");
            }

            return modifiedText; // Возвращаем текст с префиксом для отладки
        }

        // Вспомогательный метод для логирования
        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] {message}\n");
            }
            catch { }
        }
    }
}