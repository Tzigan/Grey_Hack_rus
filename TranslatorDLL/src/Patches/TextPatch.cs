using GreyHackTranslator;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
            if (!string.IsNullOrEmpty(value))
            {
                string original = value;
                value = TranslatorPlugin.TranslateText(value);

                // Логируем перевод только если он изменился и не логировали этот текст ранее
                if (value != original)
                {
                    int hash = original.GetHashCode();

                    // Выводим информацию о компоненте не для каждого текста (чтобы не спамить)
                    if (!processedTexts.Contains(hash))
                    {
                        processedTexts.Add(hash);

                        // Собираем информацию о компоненте UI
                        string gameObjectPath = GetGameObjectPath(__instance.transform);

                        Debug.Log($"{DEBUG_PREFIX}Перевод UI на объекте '{gameObjectPath}': '{original}' -> '{value}'");
                        TranslatorPlugin.Log($"Перевод UI на объекте '{gameObjectPath}': '{original}' -> '{value}'");

                        // Ограничиваем размер кэша
                        if (processedTexts.Count > 1000)
                        {
                            processedTexts.Clear();
                        }
                    }
                }
            }
        }

        // Получаем путь к объекту в иерархии для отладки
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