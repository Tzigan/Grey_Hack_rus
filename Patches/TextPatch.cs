using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace GreyHackRussianPlugin.Patches
{
    [HarmonyPatch(typeof(Text), "set_text")]
    public class TextPatch
    {
        private static readonly HashSet<int> processedTexts = new HashSet<int>();

        static void Prefix(ref string value, Text __instance)
        {
            if (string.IsNullOrEmpty(value)) return;

            try
            {
                string original = value;

                // Переводим текст
                value = GreyHackRussianPlugin.Translation.Translator.TranslateText(original);

                if (value != original)
                {
                    int hash = original.GetHashCode();

                    // Ограничиваем логирование
                    if (!processedTexts.Contains(hash))
                    {
                        processedTexts.Add(hash);
                        GreyHackRussian.GreyHackRussianPlugin.Log.LogInfo($"UI Text переведен: '{original}' -> '{value}' на объекте '{__instance?.gameObject?.name}'");
                    }

                    // Ограничиваем размер кэша
                    if (processedTexts.Count > 1000)
                    {
                        processedTexts.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                GreyHackRussian.GreyHackRussianPlugin.Log.LogError($"Ошибка в TextPatch: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}