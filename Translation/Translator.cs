using System;
using System.Collections.Generic;
using System.IO;

namespace GreyHackRussian
{
    public static class Translator
    {
        public static Dictionary<string, string> TranslationDictionary = new Dictionary<string, string>();
        private static string translationFilePath;

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

                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        TranslationDictionary[key] = value;
                        count++;
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

            // Записываем непереведенные строки в отдельный файл в подпапке Translation
            try
            {
                if (original.Length > 2)
                {
                    // Путь к непереведенным строкам в подпапке Translation
                    string translationDirectory = Path.Combine(GreyHackRussianPlugin.PluginPath, "Translation");
                    if (!Directory.Exists(translationDirectory))
                    {
                        Directory.CreateDirectory(translationDirectory);
                    }

                    string untranslatedPath = Path.Combine(translationDirectory, "untranslated.txt");

                    // Проверяем, есть ли уже эта строка в файле
                    bool shouldAppend = true;
                    if (File.Exists(untranslatedPath))
                    {
                        string content = File.ReadAllText(untranslatedPath);
                        shouldAppend = !content.Contains(original);
                    }

                    if (shouldAppend)
                    {
                        File.AppendAllText(untranslatedPath, original + "\n");
                    }

                    // Выводим информацию о непереведенной строке не слишком часто
                    int hashCode = original.GetHashCode();
                    if (Math.Abs(hashCode % 50) == 0)
                    {
                        GreyHackRussianPlugin.Log.LogDebug($"Не найден перевод: '{original}'");
                    }
                }
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка записи непереведенной строки: {ex.Message}");
            }

            return original; // Возвращаем оригинальный текст, если перевода нет
        }
    }
}