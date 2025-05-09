using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GreyHackTranslator
{
    public class TranslatorPlugin
    {
        public static Dictionary<string, string> translationDictionary = new Dictionary<string, string>();
        private static bool isInitialized = false;
        private static string translationFilePath = "russian_translation.txt";
        private static string logPath = "translator_log.txt";

        // Точка входа для инжектора
        public static void Init()
        {
            if (isInitialized) return;
            
            try
            {
                Log("Инициализация переводчика...");
                
                // Загружаем переводы из имеющегося файла переводов
                LoadTranslations();
                
                // Создаем экземпляр Harmony для патчинга
                var harmony = new Harmony("com.tzigan.greyhack.rus");
                
                // Применяем все патчи в этой сборке
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                
                Log("Переводчик успешно инициализирован");
                isInitialized = true;
            }
            catch (Exception ex)
            {
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
                        }
                    }
                    
                    Log($"Загружено {count} строк перевода из {translationFilePath}");
                }
                else
                {
                    Log($"Файл перевода не найден: {Path.GetFullPath(translationFilePath)}");
                    
                    // Создаем пустой файл перевода
                    File.WriteAllText(translationFilePath, "# Формат: оригинальный текст=переведенный текст\n");
                    Log("Создан пустой файл перевода");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка загрузки переводов: {ex.Message}");
            }
        }
        
        // Метод для перевода текста
        public static string TranslateText(string original)
        {
            if (string.IsNullOrEmpty(original)) return original;
            
            if (translationDictionary.TryGetValue(original, out string translation))
            {
                return translation;
            }
            
            // Записываем непереведенные строки в отдельный файл
            try 
            {
                File.AppendAllText("untranslated.txt", original + "\n");
            }
            catch {}
            
            return original;
        }
        
        // Вспомогательный метод для логирования
        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] {message}\n");
            }
            catch {}
        }
    }
}