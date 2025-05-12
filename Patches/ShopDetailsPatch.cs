using GreyHackRussianPlugin.Translation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace GreyHackRussianPlugin.Patches
{
    /// <summary>
    /// Патч для перевода технических терминов в окне Details магазина
    /// </summary>
    [HarmonyPatch]
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

        // Патчим несколько возможных методов, которые могут генерировать HTML для окна Details

        // Вариант 1: Метод GetDetailsHTML
        [HarmonyPatch(typeof(ItemShopAdvanced), "GetDetailsHTML")]
        [HarmonyPostfix]
        public static void GetDetailsHTMLPostfix(ref string __result)
        {
            TranslateHtml(ref __result, "GetDetailsHTML");
        }

        // Вариант 2: Метод GetDetailsHtml (с другим регистром)
        [HarmonyPatch(typeof(ItemShopAdvanced), "GetDetailsHtml")]
        [HarmonyPostfix]
        public static void GetDetailsHtmlPostfix(ref string __result)
        {
            TranslateHtml(ref __result, "GetDetailsHtml");
        }

        // Вариант 3: Метод в ItemShopHardware
        [HarmonyPatch(typeof(ItemShopHardware), "GetDetailsHTML")]
        [HarmonyPostfix]
        public static void ItemShopHardwareGetDetailsHTMLPostfix(ref string __result)
        {
            TranslateHtml(ref __result, "ItemShopHardware.GetDetailsHTML");
        }

        // Вариант 4: Метод, который конфигурирует браузер (с проверкой контекста)
        [HarmonyPatch(typeof(HtmlBrowser), "SetHTML")]
        [HarmonyPrefix]
        public static void HtmlBrowserSetHTMLPrefix(HtmlBrowser __instance, ref string html)
        {
            // Проверяем, находимся ли мы в контексте магазина
            bool isShopContext = __instance.transform.root.name.Contains("Shop") ||
                (__instance.transform.parent != null && __instance.transform.parent.name.Contains("Item"));

            if (isShopContext)
            {
                TranslateHtml(ref html, "HtmlBrowser.SetHTML");
            }
        }

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
            GreyHackRussianPlugin.Log.LogInfo("ShopDetailsPatch инициализирован с поддержкой перевода технических терминов магазина");
        }
    }
}