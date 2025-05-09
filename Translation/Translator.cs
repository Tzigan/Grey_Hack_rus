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
            // Путь к файлу переводов в папке плагина
            translationFilePath = Path.Combine(GreyHackRussianPlugin.PluginPath, "russian_translation.txt");

            GreyHackRussianPlugin.Log.LogInfo($"Загрузка переводов из {translationFilePath}");

            if (File.Exists(translationFilePath))
            {
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

                        // Вывод первых 5 переводов для отладки
                        if (count <= 5)
                        {
                            GreyHackRussianPlugin.Log.LogDebug($"Загружен перевод: '{key}' -> '{value}'");
                        }
                    }
                }

                GreyHackRussianPlugin.Log.LogInfo($"Загружено {count} строк перевода");
            }
            else
            {
                GreyHackRussianPlugin.Log.LogWarning($"Файл перевода не найден: {translationFilePath}");

                // Создаем пустой файл перевода
                File.WriteAllText(translationFilePath, "# Формат: оригинальный текст=переведенный текст\n");
                GreyHackRussianPlugin.Log.LogInfo($"Создан пустой файл перевода");
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

            // Записываем непереведенные строки в отдельный файл
            try
            {
                // Не логируем слишком короткие строки
                if (original.Length > 2)
                {
                    string untranslatedPath = Path.Combine(GreyHackRussianPlugin.PluginPath, "untranslated.txt");

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