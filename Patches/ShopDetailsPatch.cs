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
        public static void Initialize()
        {
            GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Начало инициализации...");

            try
            {
                // Проверка классов магазина, чтобы не вызывать ошибок
                Type itemShopAdvanced = AccessTools.TypeByName("ItemShopAdvanced");
                Type itemShopHardware = AccessTools.TypeByName("ItemShopHardware");

                if (itemShopAdvanced != null)
                {
                    GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Найден класс ItemShopAdvanced");
                }

                if (itemShopHardware != null)
                {
                    GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Найден класс ItemShopHardware");
                }

                GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch успешно инициализирован");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при инициализации ShopDetailsPatch: {ex.Message}");
            }
        }

        // Общий постфикс для всех методов, возвращающих HTML
        [HarmonyPatch]
        public class DetailsPatchGroup
        {
            static MethodBase TargetMethod()
            {
                // Попытка найти ItemShopAdvanced.GetDetailsHTML
                Type type = AccessTools.TypeByName("ItemShopAdvanced");
                if (type != null)
                {
                    MethodInfo method = AccessTools.Method(type, "GetDetailsHTML");
                    if (method != null)
                    {
                        GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Применяем патч к ItemShopAdvanced.GetDetailsHTML");
                        return method;
                    }
                }

                // Если не найден, пробуем GetDetailsHtml
                if (type != null)
                {
                    MethodInfo method = AccessTools.Method(type, "GetDetailsHtml");
                    if (method != null)
                    {
                        GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Применяем патч к ItemShopAdvanced.GetDetailsHtml");
                        return method;
                    }
                }

                // Если не найден, пробуем ItemShopHardware
                type = AccessTools.TypeByName("ItemShopHardware");
                if (type != null)
                {
                    MethodInfo method = AccessTools.Method(type, "GetDetailsHTML");
                    if (method != null)
                    {
                        GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch: Применяем патч к ItemShopHardware.GetDetailsHTML");
                        return method;
                    }
                }

                // Если ничего не найдено, возвращаем null, чтобы отключить патч
                GreyHackRussianPlugin.Log.LogWarning("ShopDetailsPatch: Не найдены подходящие методы для патча");
                return null;
            }

            static void Postfix(ref string __result)
            {
                TranslateHtml(ref __result, "DetailsHTML");
            }
        }
    }
}