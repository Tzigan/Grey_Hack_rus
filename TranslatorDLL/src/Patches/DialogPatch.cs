using GreyHackTranslator;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace GreyHackTranslator.Patches
{
    // Здесь нужно будет адаптировать патчи под конкретные классы в игре Grey Hack

    [HarmonyPatch]
    public class DialogPatch
    {
        private const string DEBUG_PREFIX = "[GH_RUS_DIALOG] ";

        static MethodBase TargetMethod()
        {
            Debug.Log($"{DEBUG_PREFIX}Поиск метода диалога...");

            // Пример для метода ShowDialog в классе DialogManager
            // Нужно заменить на реальные классы из игры
            var type = AccessTools.TypeByName("DialogManager");
            if (type != null)
            {
                Debug.Log($"{DEBUG_PREFIX}Найден тип DialogManager");
                var method = AccessTools.Method(type, "ShowDialog");
                if (method != null)
                {
                    Debug.Log($"{DEBUG_PREFIX}Найден метод ShowDialog");
                    return method;
                }
                else
                {
                    Debug.LogWarning($"{DEBUG_PREFIX}Метод ShowDialog не найден");
                }
            }
            else
            {
                Debug.LogWarning($"{DEBUG_PREFIX}Тип DialogManager не найден");
            }

            // Поиск альтернативных методов для диалогов
            Debug.Log($"{DEBUG_PREFIX}Поиск альтернативных методов диалога...");

            // Перебираем все типы в игре, пытаясь найти подходящие
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.GetName().Name == "Assembly-CSharp")
                {
                    Debug.Log($"{DEBUG_PREFIX}Анализ Assembly-CSharp...");

                    // Пример поиска классов, которые могут содержать функционал диалогов
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        string typeName = t.Name.ToLower();
                        if (typeName.Contains("dialog") || typeName.Contains("message") ||
                            typeName.Contains("ui") || typeName.Contains("text"))
                        {
                            Debug.Log($"{DEBUG_PREFIX}Найден потенциальный тип: {t.FullName}");
                        }
                    }
                }
            }

            return null;
        }

        static void Prefix(ref string __0)
        {
            if (__0 != null)
            {
                Debug.Log($"{DEBUG_PREFIX}Обработка текста диалога: '{__0}'");
                string original = __0;
                __0 = TranslatorPlugin.TranslateText(__0);

                if (__0 != original)
                {
                    Debug.Log($"{DEBUG_PREFIX}Перевод диалога: '{original}' -> '{__0}'");
                    TranslatorPlugin.Log($"Перевод диалога: '{original}' -> '{__0}'");
                }
            }
        }
    }
}