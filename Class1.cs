using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace GreyHackTranslator
{
    public class TranslatorPlugin
    {
        private static Dictionary<string, string> translationDictionary = new Dictionary<string, string>();
        private static bool isInitialized = false;
        private static string translationFilePath = "translations.txt";

        // Точка входа для инжектора
        public static void Init()
        {
            if (isInitialized) return;
            
            try
            {
                LoadTranslations();
                
                // Создаем экземпляр Harmony для патчинга
                var harmony = new Harmony("com.translator.greyhack");
                
                // Применяем все патчи в этой сборке
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                
                // Запись в лог об успешной загрузке
                File.AppendAllText("translator_log.txt", $"[{DateTime.Now}] Translator loaded successfully\n");
                
                isInitialized = true;
            }
            catch (Exception ex)
            {
                File.AppendAllText("translator_error.txt", $"[{DateTime.Now}] Error: {ex}\n");
            }
        }
        
        // Загрузка переводов из файла
        private static void LoadTranslations()
        {
            if (File.Exists(translationFilePath))
            {
                string[] lines = File.ReadAllLines(translationFilePath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line) || !line.Contains("=")) continue;
                    
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        translationDictionary[key] = value;
                    }
                }
                
                File.AppendAllText("translator_log.txt", $"[{DateTime.Now}] Loaded {translationDictionary.Count} translations\n");
            }
            else
            {
                File.AppendAllText("translator_log.txt", $"[{DateTime.Now}] Translation file not found at {Path.GetFullPath(translationFilePath)}\n");
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
            
            // Запись непереведенных строк для дальнейшего добавления в словарь
            File.AppendAllText("untranslated.txt", original + "\n");
            
            return original;
        }
    }
    
    // Примеры патчей для перехвата отображения текста
    
    // Патч для UI.Text компонентов
    [HarmonyPatch(typeof(UnityEngine.UI.Text), "set_text")]
    public class TextPatch
    {
        static void Prefix(ref string value)
        {
            value = TranslatorPlugin.TranslateText(value);
        }
    }
    
    // Патч для предполагаемого класса диалогов в игре
    [HarmonyPatch]
    public class DialogPatch
    {
        // Метод для определения целевого метода
        static MethodBase TargetMethod()
        {
            // Здесь нужно найти класс и метод, отвечающий за отображение диалогов
            // Например, если есть класс DialogManager с методом ShowDialog:
            var type = AccessTools.TypeByName("DialogManager");
            if (type != null)
            {
                return AccessTools.Method(type, "ShowDialog");
            }
            return null;
        }
        
        static void Prefix(ref string __0) // __0 - первый аргумент метода
        {
            if (__0 != null)
            {
                __0 = TranslatorPlugin.TranslateText(__0);
            }
        }
    }
}
