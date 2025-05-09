using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        // Добавим константу для отладки
        private const string DEBUG_PREFIX = "[GH_RUS] ";

        // Точка входа для инжектора
        public static void Init()
        {
            if (isInitialized) return;

            try
            {
                // Добавляем отладочный вывод
                Debug.Log($"{DEBUG_PREFIX}Инициализация переводчика...");
                Log("Инициализация переводчика...");

                // Получаем информацию о версии Harmony
                string harmonyVersion = typeof(Harmony).Assembly.GetName().Version.ToString();
                Debug.Log($"{DEBUG_PREFIX}Версия Harmony: {harmonyVersion}");
                Log($"Версия Harmony: {harmonyVersion}");

                // Выводим текущую директорию и путь к файлу перевода
                string currentDir = Directory.GetCurrentDirectory();
                Debug.Log($"{DEBUG_PREFIX}Текущая директория: {currentDir}");
                Debug.Log($"{DEBUG_PREFIX}Путь к файлу перевода: {Path.GetFullPath(translationFilePath)}");
                Log($"Текущая директория: {currentDir}");

                // Загружаем переводы из имеющегося файла переводов
                LoadTranslations();

                // Информация о загруженных сборках
                Debug.Log($"{DEBUG_PREFIX}Загруженные сборки:");
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Debug.Log($"{DEBUG_PREFIX}  - {assembly.GetName().Name} v{assembly.GetName().Version}");
                }

                // Создаем экземпляр Harmony для патчинга
                Debug.Log($"{DEBUG_PREFIX}Создаем экземпляр Harmony...");
                var harmony = new Harmony("com.tzigan.greyhack.rus");

                // Применяем все патчи в этой сборке
                Debug.Log($"{DEBUG_PREFIX}Применяем патчи...");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log($"{DEBUG_PREFIX}Патчи успешно применены");
                Log("Патчи успешно применены");

                // Альтернативно, можно получить информацию о патчах через другой API
                var patchedMethods = Harmony.GetAllPatchedMethods().ToList();
                Debug.Log($"{DEBUG_PREFIX}Применено патчей к {patchedMethods.Count} методам");
                Log($"Применено патчей к {patchedMethods.Count} методам");

                foreach (var method in patchedMethods)
                {
                    Debug.Log($"{DEBUG_PREFIX}Патч к методу: {method.DeclaringType.Name}.{method.Name}");
                    Log($"Патч к методу: {method.DeclaringType.Name}.{method.Name}");
                }

                Debug.Log($"{DEBUG_PREFIX}Переводчик успешно инициализирован");
                Log("Переводчик успешно инициализирован");
                isInitialized = true;
            }
            catch (Exception ex)
            {
                // Выводим ошибку в консоль Unity
                Debug.LogError($"{DEBUG_PREFIX}Ошибка инициализации: {ex.Message}");
                Debug.LogError($"{DEBUG_PREFIX}Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Debug.LogError($"{DEBUG_PREFIX}InnerException: {ex.InnerException.Message}");
                    Debug.LogError($"{DEBUG_PREFIX}InnerException stack trace: {ex.InnerException.StackTrace}");
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
                    Debug.Log($"{DEBUG_PREFIX}Загрузка переводов из {translationFilePath}...");
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

                            // Выводим первые 10 переводов для отладки
                            if (count <= 10)
                            {
                                Debug.Log($"{DEBUG_PREFIX}Загружен перевод: '{key}' -> '{value}'");
                            }
                        }
                    }

                    Debug.Log($"{DEBUG_PREFIX}Загружено {count} строк перевода");
                    Log($"Загружено {count} строк перевода из {translationFilePath}");
                }
                else
                {
                    Debug.LogWarning($"{DEBUG_PREFIX}Файл перевода не найден: {Path.GetFullPath(translationFilePath)}");
                    Log($"Файл перевода не найден: {Path.GetFullPath(translationFilePath)}");

                    // Создаем пустой файл перевода
                    File.WriteAllText(translationFilePath, "# Формат: оригинальный текст=переведенный текст\n");
                    Debug.Log($"{DEBUG_PREFIX}Создан пустой файл перевода");
                    Log("Создан пустой файл перевода");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{DEBUG_PREFIX}Ошибка загрузки переводов: {ex.Message}");
                Debug.LogError($"{DEBUG_PREFIX}Stack trace: {ex.StackTrace}");
                Log($"Ошибка загрузки переводов: {ex.Message}");
            }
        }

        // Метод для перевода текста
        public static string TranslateText(string original)
        {
            if (string.IsNullOrEmpty(original)) return original;

            if (translationDictionary.TryGetValue(original, out string translation))
            {
                // Добавим сообщение, только если не слишком часто вызывается
                // для часто используемых строк (каждый 100-й раз)
                int hashCode = original.GetHashCode();
                if (Math.Abs(hashCode % 100) == 0)
                {
                    Debug.Log($"{DEBUG_PREFIX}Перевод: '{original}' -> '{translation}'");
                }
                return translation;
            }

            // Записываем непереведенные строки в отдельный файл
            try
            {
                // Не логируем слишком короткие строки (скорее всего одиночные символы)
                if (original.Length > 2 && !IsNumeric(original))
                {
                    File.AppendAllText("untranslated.txt", original + "\n");

                    // Выводим информацию о непереведенной строке не слишком часто
                    int hashCode = original.GetHashCode();
                    if (Math.Abs(hashCode % 50) == 0)
                    {
                        Debug.LogWarning($"{DEBUG_PREFIX}Не найден перевод: '{original}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{DEBUG_PREFIX}Ошибка записи непереведенной строки: {ex.Message}");
            }

            return original;
        }

        // Проверка, является ли строка числом
        private static bool IsNumeric(string text)
        {
            double number;
            return double.TryParse(text, out number);
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