using HarmonyLib;
using GreyHackTranslator;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace GreyHackTranslator.Patches
{
    // Патч для стандартных UI.Text компонентов Unity
    [HarmonyPatch(typeof(Text), "set_text")]
    public class TextPatch
    {
        private static readonly HashSet<int> processedTexts = new HashSet<int>();
        private const string DEBUG_PREFIX = "[GH_RUS_UI] ";

        static void Prefix(ref string value, Text __instance)
        {
            if (string.IsNullOrEmpty(value)) return;

            try
            {
                string original = value;
                TranslatorPlugin.EmergencyLog($"UI Text до перевода: '{original}' на объекте '{__instance?.gameObject?.name}'");

                // Переводим текст
                value = TranslatorPlugin.TranslateText(original);

                if (value != original)
                {
                    int hash = original.GetHashCode();

                    // Ограничиваем логирование
                    if (!processedTexts.Contains(hash))
                    {
                        processedTexts.Add(hash);

                        try
                        {
                            Debug.Log($"{DEBUG_PREFIX}Перевод UI: '{original}' -> '{value}'");
                        }
                        catch { }

                        TranslatorPlugin.EmergencyLog($"UI Text переведен: '{original}' -> '{value}' на объекте '{__instance?.gameObject?.name}'");
                    }

                    // Ограничиваем размер кэша
                    if (processedTexts.Count > 1000)
                    {
                        processedTexts.Clear();
                    }
                }
            }
            catch (System.Exception ex)
            {
                TranslatorPlugin.EmergencyLog($"Ошибка в TextPatch: {ex.Message}\n{ex.StackTrace}");

                // В случае ошибки не меняем оригинальное значение
                value = value;
            }
        }
    }
}