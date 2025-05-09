using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace GreyHackTranslator
{
    public static class UIAnalyzer
    {
        private static bool isAnalyzing = false;
        private const string DEBUG_PREFIX = "[GH_RUS_ANALYZER] ";

        // Метод для запуска анализа UI
        [HarmonyPatch(typeof(UnityEngine.Time), "get_frameCount")]
        public class FrameCountPatch
        {
            static void Postfix(ref int __result)
            {
                // Анализируем UI каждые 1000 кадров
                if (__result % 1000 == 0 && !isAnalyzing)
                {
                    try
                    {
                        isAnalyzing = true;
                        AnalyzeUI();
                    }
                    finally
                    {
                        isAnalyzing = false;
                    }
                }
            }
        }

        private static void AnalyzeUI()
        {
            Debug.Log($"{DEBUG_PREFIX}Начинаем анализ UI элементов...");

            // Находим все текстовые компоненты в активных сценах
            var allTexts = UnityEngine.Object.FindObjectsOfType<Text>();
            Debug.Log($"{DEBUG_PREFIX}Найдено {allTexts.Length} Text компонентов");

            int count = 0;
            foreach (var text in allTexts)
            {
                // Ограничиваем вывод до 20 компонентов за раз
                if (count++ < 20)
                {
                    string content = text.text;
                    // Обрезаем слишком длинные строки
                    if (content.Length > 50)
                        content = content.Substring(0, 47) + "...";

                    string gameObjectPath = GetGameObjectPath(text.transform);
                    Debug.Log($"{DEBUG_PREFIX}Text[{count}] на '{gameObjectPath}': '{content}'");
                }
            }

            // Пытаемся найти другие типы текстовых компонентов
            try
            {
                var tmpTexts = UnityEngine.Object.FindObjectsOfType(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro"));
                if (tmpTexts != null)
                {
                    Debug.Log($"{DEBUG_PREFIX}Найдено {tmpTexts.Length} TextMeshPro компонентов");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{DEBUG_PREFIX}TextMeshPro не используется: {ex.Message}");
            }

            Debug.Log($"{DEBUG_PREFIX}Анализ UI завершен");
        }

        private static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}