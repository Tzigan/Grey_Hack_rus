using GreyHackRussianPlugin.Translation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace GreyHackRussianPlugin.Patches
{
    /// <summary>
    /// Патч для перевода технических терминов в окне Details магазина
    /// </summary>
    public class ShopDetailsPatch
    {
        // Словарь замен для HTML-контента
        private static readonly Dictionary<string, string> htmlReplacements = new Dictionary<string, string>
        {
            // Базовые характеристики
            { "<b>Size:</b>", "<b>Размер:</b>" },
            { "<b>Size: </b>", "<b>Размер: </b>" },
            { "<b>Speed:</b>", "<b>Скорость:</b>" },
            { "<b>Speed: </b>", "<b>Скорость: </b>" },
            { "<b>Quality:</b>", "<b>Качество:</b>" },
            { "<b>Quality: </b>", "<b>Качество: </b>" },
            
            // Процессор
            { "<b>Cores:</b>", "<b>Ядра:</b>" },
            { "<b>Cores: </b>", "<b>Ядра: </b>" },
            { "<b>Socket:</b>", "<b>Сокет:</b>" },
            { "<b>Socket :</b>", "<b>Сокет:</b>" },
            
            // Видеокарта
            { "<b>Hashrate:</b>", "<b>Хешрейт:</b>" },
            { "<b>Hashrate: </b>", "<b>Хешрейт: </b>" },
            
            // Память
            { "<b>Memory:</b>", "<b>Память:</b>" },
            { "<b>Memory: </b>", "<b>Память: </b>" },
            { "<b>Model:</b>", "<b>Модель:</b>" },
            { "<b>Model: </b>", "<b>Модель: </b>" },
            
            // Материнская плата
            { "<b>CPUs:</b>", "<b>Процессоры:</b>" },
            { "<b>CPUs: </b>", "<b>Процессоры: </b>" },
            { "<b>RAMs:</b>", "<b>Память:</b>" },
            { "<b>RAMs: </b>", "<b>Память: </b>" },
            { "<b>Max socket RAM:</b>", "<b>Макс. RAM на сокет:</b>" },
            { "<b>Max socket RAM: </b>", "<b>Макс. RAM на сокет: </b>" },
            { "<b>PCIs:</b>", "<b>PCI слоты:</b>" },
            { "<b>PCIs: </b>", "<b>PCI слоты: </b>" },
            
            // Блок питания
            { "<b>Power:</b>", "<b>Мощность:</b>" },
            { "<b>Power: </b>", "<b>Мощность: </b>" },
            
            // Сетевые карты
            { "<b>Monitor support:</b>", "<b>Поддержка мониторинга:</b>" },
            { "<b>Monitor support: </b>", "<b>Поддержка мониторинга: </b>" },
            { "<b>Ethernet card</b>", "<b>Ethernet карта</b>" },
            { "<b>WiFi card</b>", "<b>WiFi карта</b>" },
            
            // Прочее
            { " Yes", " Да" },
            { " No", " Нет" }
        };

        // Общий метод для перевода HTML
        private static void TranslateHtml(ref string html, string source)
        {
            if (string.IsNullOrEmpty(html))
                return;

            string originalHtml = html;

            // Применяем все замены
            foreach (var replacement in htmlReplacements)
            {
                html = html.Replace(replacement.Key, replacement.Value);
            }

            // Если что-то изменилось, логируем это
            if (html != originalHtml)
            {
                GreyHackRussianPlugin.Log.LogInfo($"ShopDetailsPatch: HTML-контент успешно переведен из {source}");
            }
        }

        // Метод инициализации патча
        public static void Initialize(Harmony harmony)
        {
            GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Начало инициализации...");

            try
            {
                // Динамически ищем и патчим методы только если они существуют
                RegisterPatchForMethod(harmony, "ItemShopAdvanced", "GetDetailsHTML");
                RegisterPatchForMethod(harmony, "ItemShopAdvanced", "GetDetailsHtml");
                RegisterPatchForMethod(harmony, "ItemShopHardware", "GetDetailsHTML");

                GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch успешно инициализирован");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при инициализации ShopDetailsPatch: {ex.Message}");
            }
        }

        // Вспомогательный метод для регистрации патча только если метод существует
        private static void RegisterPatchForMethod(Harmony harmony, string typeName, string methodName)
        {
            try
            {
                // Ищем тип по имени
                Type type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    GreyHackRussianPlugin.Log.LogWarning($"ShopDetailsPatch: Тип '{typeName}' не найден");
                    return;
                }

                // Ищем метод
                MethodInfo method = AccessTools.Method(type, methodName);
                if (method == null)
                {
                    GreyHackRussianPlugin.Log.LogWarning($"ShopDetailsPatch: Метод '{methodName}' не найден в типе '{typeName}'");
                    return;
                }

                // Создаем метод для постфикса с правильной сигнатурой
                MethodInfo postfixMethod = typeof(ShopDetailsPatch).GetMethod(nameof(TranslateDetailsHTML), BindingFlags.Static | BindingFlags.Public);

                // Патчим найденный метод
                HarmonyMethod postfix = new HarmonyMethod(postfixMethod);
                harmony.Patch(method, null, postfix);
                GreyHackRussianPlugin.Log.LogInfo($"ShopDetailsPatch: Успешно зарегистрирован патч для {typeName}.{methodName}");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogWarning($"ShopDetailsPatch: Не удалось зарегистрировать патч для {typeName}.{methodName}: {ex.Message}");
            }
        }

        // Общий постфикс для всех методов, возвращающих HTML
        public static void TranslateDetailsHTML(ref string __result)
        {
            try
            {
                TranslateHtml(ref __result, "DetailsHTML");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"ShopDetailsPatch: Ошибка перевода HTML: {ex.Message}");
            }
        }
    }
}