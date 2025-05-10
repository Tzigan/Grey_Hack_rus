using System;
using System.Collections.Generic;
using System.IO;

namespace GreyHackRussianPlugin.Translation
{
    public static class Translator
    {
        public static Dictionary<string, string> TranslationDictionary = new Dictionary<string, string>();
        private static string translationFilePath;

        /// <summary>
        /// Загружает переводы из файла
        /// </summary>
        public static void LoadTranslations()
        {
            // Сначала создаем директорию Translation, если нужно
            string translationDirectory = Path.Combine(GreyHackRussianPlugin.PluginPath, "Translation");
            if (!Directory.Exists(translationDirectory))
            {
                Directory.CreateDirectory(translationDirectory);
                GreyHackRussianPlugin.Log.LogInfo($"Создана директория для переводов: {translationDirectory}");
            }

            // Теперь путь к файлу переводов указывает на подпапку Translation
            translationFilePath = Path.Combine(translationDirectory, "russian_translation.txt");

            GreyHackRussianPlugin.Log.LogInfo($"Загрузка переводов из {translationFilePath}");

            if (File.Exists(translationFilePath))
            {
                // Загрузка переводов из файла
                string[] lines = File.ReadAllLines(translationFilePath);
                int count = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || !line.Contains("=")) continue;

                    // Лучшее разделение на ключ и значение, с поддержкой экранированных знаков равенства
                    int equalPos = line.IndexOf('=');
                    if (equalPos > 0)
                    {
                        string key = line.Substring(0, equalPos).Trim();
                        string value = line.Substring(equalPos + 1).Trim();
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            TranslationDictionary[key] = value;
                            count++;
                        }
                    }
                }

                GreyHackRussianPlugin.Log.LogInfo($"Загружено {count} строк перевода");
            }
            else
            {
                // Создаем пустой файл перевода в подпапке Translation
                File.WriteAllText(translationFilePath, "# Формат: оригинальный текст=переведенный текст\n");
                GreyHackRussianPlugin.Log.LogInfo($"Создан пустой файл перевода в {translationFilePath}");
            }
        }

        /// <summary>
        /// Перезагружает переводы из файла
        /// </summary>
        public static void ReloadTranslations()
        {
            TranslationDictionary.Clear();
            LoadTranslations();
            GreyHackRussianPlugin.Log.LogInfo("Переводы перезагружены");
        }

        /// <summary>
        /// Переводит текст с использованием словаря переводов
        /// </summary>
        /// <param name="original">Оригинальный текст</param>
        /// <returns>Переведенный текст или оригинал, если перевод не найден</returns>
        public static string TranslateText(string original)
        {
            if (string.IsNullOrEmpty(original)) return original;

            // Если есть перевод в словаре, используем его
            if (TranslationDictionary.TryGetValue(original, out string translation))
            {
                // Запись успешного перевода в лог (нечасто)
                int hashCode = original.GetHashCode();
                if (Math.Abs(hashCode % 100) == 0)
                {
                    GreyHackRussianPlugin.Log.LogDebug($"Перевод: '{original}' -> '{translation}'");
                }
                return translation;
            }

            // Для коротких строк (меньше 3 символов) просто возвращаем оригинал
            if (original.Length <= 2)
                return original;

            // Для более длинных строк - логируем информацию о непереведенной строке (не слишком часто)
            int logHashCode = original.GetHashCode();
            if (Math.Abs(logHashCode % 50) == 0)
            {
                GreyHackRussianPlugin.Log.LogDebug($"Не найден перевод: '{original}'");
            }

            return original; // Возвращаем оригинальный текст, если перевода нет
        }

        /// <summary>
        /// Метод для поиска перевода без учета регистра и пробелов
        /// </summary>
        /// <param name="original">Оригинальный текст</param>
        /// <returns>Переведенный текст или оригинал, если перевод не найден</returns>
        public static string TranslateTextIgnoreCase(string original)
        {
            if (string.IsNullOrEmpty(original)) return original;

            // Сначала ищем точное соответствие через стандартный метод
            if (TranslationDictionary.TryGetValue(original, out string translation))
            {
                return translation;
            }

            // Затем ищем без учета регистра и пробелов
            foreach (var kvp in TranslationDictionary)
            {
                // Проверка без учета регистра
                if (string.Equals(kvp.Key, original, StringComparison.OrdinalIgnoreCase))
                {
                    GreyHackRussianPlugin.Log.LogInfo($"Найден перевод без учета регистра: '{original}' -> '{kvp.Value}'");
                    return kvp.Value;
                }

                // Проверка без учета пробелов в начале/конце и регистра
                if (string.Equals(kvp.Key.Trim(), original.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    GreyHackRussianPlugin.Log.LogInfo($"Найден перевод без учета пробелов: '{original}' -> '{kvp.Value}'");
                    return kvp.Value;
                }
            }

            return original;
        }
    }
}