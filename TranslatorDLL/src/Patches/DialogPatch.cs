using HarmonyLib;
using System;
using System.Reflection;
using GreyHackTranslator;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GreyHackTranslator.Patches
{
    // Патч для поиска и обработки текстовых методов в игре
    [HarmonyPatch]
    public class TextFinderPatch
    {
        private const string DEBUG_PREFIX = "[GH_RUS_FINDER] ";

        // Этот метод ищет подходящий метод для патчинга
        static MethodBase TargetMethod()
        {
            try
            {
                TranslatorPlugin.EmergencyLog("Поиск методов для патчинга...");

                // Искать классы, которые могут содержать текст
                Type[] potentialTypes = {
                    // Проверяем несколько вариантов имен классов, которые могут отвечать за UI
                    Type.GetType("GameManager, Assembly-CSharp"),
                    Type.GetType("UIManager, Assembly-CSharp"),
                    Type.GetType("TextManager, Assembly-CSharp"),
                    Type.GetType("ChatManager, Assembly-CSharp")
                };

                foreach (var type in potentialTypes)
                {
                    if (type != null)
                    {
                        TranslatorPlugin.EmergencyLog($"Найден тип: {type.FullName}");

                        // Ищем методы, которые могут устанавливать текст
                        var setTextMethod = AccessTools.Method(type, "SetText");
                        if (setTextMethod != null)
                        {
                            TranslatorPlugin.EmergencyLog($"Найден метод: {type.Name}.SetText");
                            return setTextMethod;
                        }

                        // Альтернативные названия методов
                        string[] methodNames = {
                            "ShowMessage", "DisplayText", "SetDialogText",
                            "UpdateText", "PrintText", "ShowText"
                        };

                        foreach (var methodName in methodNames)
                        {
                            var method = AccessTools.Method(type, methodName);
                            if (method != null)
                            {
                                TranslatorPlugin.EmergencyLog($"Найден метод: {type.Name}.{methodName}");
                                return method;
                            }
                        }
                    }
                }

                // Если не нашли в известных классах, поищем во всех классах игры
                TranslatorPlugin.EmergencyLog("Поиск методов в Assembly-CSharp...");
                var gameAssembly = Assembly.Load("Assembly-CSharp");
                foreach (var type in gameAssembly.GetTypes())
                {
                    // Пропускаем типы, имя которых не похоже на обработчик текста
                    if (!type.Name.Contains("Text") &&
                        !type.Name.Contains("UI") &&
                        !type.Name.Contains("Dialog") &&
                        !type.Name.Contains("Chat") &&
                        !type.Name.Contains("Message"))
                        continue;

                    TranslatorPlugin.EmergencyLog($"Проверка типа: {type.Name}");

                    // Проверяем, есть ли интересные методы
                    foreach (var method in type.GetMethods())
                    {
                        if ((method.Name.Contains("Set") || method.Name.Contains("Show") ||
                             method.Name.Contains("Display") || method.Name.Contains("Print")) &&
                            method.GetParameters().Length > 0)
                        {
                            var parameters = method.GetParameters();
                            foreach (var param in parameters)
                            {
                                if (param.ParameterType == typeof(string))
                                {
                                    TranslatorPlugin.EmergencyLog($"Потенциальный метод: {type.Name}.{method.Name}");
                                }
                            }
                        }
                    }
                }

                TranslatorPlugin.EmergencyLog("Не удалось найти подходящий метод для патчинга.");
                return null;
            }
            catch (Exception ex)
            {
                TranslatorPlugin.EmergencyLog($"Ошибка при поиске метода: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        static void Prefix(ref string __0)
        {
            if (__0 == null) return;

            try
            {
                string original = __0;
                TranslatorPlugin.EmergencyLog($"Dialog Text до перевода: '{original}'");

                // Переводим текст
                __0 = TranslatorPlugin.TranslateText(original);

                if (__0 != original)
                {
                    try
                    {
                        Debug.Log($"{DEBUG_PREFIX}Перевод диалога: '{original}' -> '{__0}'");
                    }
                    catch { }

                    TranslatorPlugin.EmergencyLog($"Dialog Text переведен: '{original}' -> '{__0}'");
                }
            }
            catch (Exception ex)
            {
                TranslatorPlugin.EmergencyLog($"Ошибка в DialogPatch: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}