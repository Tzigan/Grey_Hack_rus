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
            // Путь к файлу переводов в подпапке Translation
            string translationDirectory = Path.Combine(GreyHackRussianPlugin.PluginPath, "Translation");

            // Создаем директорию, если она отсутствует
            if (!Directory.Exists(translationDirectory))
            {
                Directory.CreateDirectory(translationDirectory);
                GreyHackRussianPlugin.Log.LogInfo($"Создана директория для переводов: {translationDirectory}");
            }

            translationFilePath = Path.Combine(translationDirectory, "russian_translation.txt");

            GreyHackRussianPlugin.Log.LogInfo($"Загрузка переводов из {translationFilePath}");

            if (File.Exists(translationFilePath))
            {
                // Существующий код загрузки...
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
                // Не логируем слишком короткие строки
                if (original.Length > 2)
                {
                    // Создаем директорию Translation, если она не существует
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