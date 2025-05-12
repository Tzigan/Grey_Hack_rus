using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using TMPro;
using UI.Dialogs;
using UnityEngine;
using UnityEngine.UI;
using Util;
using Path = System.IO.Path;

namespace GreyHackRussianPlugin.Patches
{
    /// <summary>
    /// Патч для перевода текстов в магазине игры
    /// </summary>
    [HarmonyPatch]
    public class ShopPatch : MonoBehaviour
    {

        // Словарь для кэширования переведенных описаний
        private static readonly Dictionary<string, string> translationCache = new Dictionary<string, string>();

        // Счетчики переведенных и непереведенных текстов
        private static int translatedCount = 0;
        private static int untranslatedCount = 0;

        // Путь к XML файлу с непереведенными текстами
        private static readonly string untranslatedXmlPath = Path.Combine(
            GreyHackRussianPlugin.PluginPath,
            "ShopPatch",
            "untranslated_shop_texts.xml"
        );

        // Порог перевода - если переведено менее X% слов, считаем текст непереведенным
        private static readonly float translationThreshold = 0.7f; // 70%

        // Список игнорируемых слов (имена, числа и т.д.)
        private static readonly HashSet<string> ignoredWords = new HashSet<string>
        {
            "mb", "gb", "tb", "rpm", "mhz", "ghz", "quality", "size", "speed",
            "power", "model", "memory", "cores", "socket", "hashrate", "mh/s",
            "cpus", "rams", "pci", "kga", "sga", "nga", "ddr1", "ddr2", "ddr3", "ddr4",
            "ethernet", "wifi", "monitor", "support", "yes", "no", "/10", "price",
            "intel", "amd", "nvidia", "rtx", "gtx", "geforce", "radeon", "ryzen"
        };

        // Список общих фраз для замены
        private static readonly Dictionary<string, string> commonPhrases = new Dictionary<string, string>
        {
            { "This device will no longer be available for sale and it will be added to your main computer storage.",
              "Это устройство больше не будет доступно для продажи и будет добавлено в хранилище вашего основного компьютера." },
            { "Do you want to continue?", "Вы хотите продолжить?" },
            { "Yes", "Да" },
            { "No", "Нет" },
            { "Details", "Подробности" },
            { "Price: $", "Цена: $" },
            { "Buy", "Купить" },
            { "Cancel", "Отмена" },
            { "Processor", "Процессор" },
            { "Graphics Card", "Видеокарта" },
            { "RAM Memory", "Оперативная память" },
            { "Hard Drive", "Жесткий диск" },
            { "Motherboard", "Материнская плата" },
            { "Power Supply", "Блок питания" },
            { "Network Card", "Сетевая карта" },
            { "unknown", "неизвестно" },
            { "Install an SSH access server on the machine.", "Установить SSH сервер на компьютер." },
            { "Install an HTTP server on the machine to host web pages.", "Установить HTTP сервер на компьютер для размещения веб-страниц." },
            { "Install an FTP access server on the machine.", "Установить FTP сервер на компьютер." },
            { "Install a chat server that can be configured as public or private.", "Установить чат-сервер, который можно настроить как публичный или приватный." },
            { "Install a repository server to host different programs.", "Установить репозиторий для хранения различных программ." }
        };

        // HTML-шаблоны для замены технических характеристик
        private static readonly Dictionary<string, string> htmlPatterns = new Dictionary<string, string>
        {
            // Базовые характеристики
            { "<b>Size:</b>", "<b>Размер:</b>" },
            { "<b>Speed:</b>", "<b>Скорость:</b>" },
            { "<b>Quality:</b>", "<b>Качество:</b>" },
            
            // Процессор
            { "<b>Cores:</b>", "<b>Ядра:</b>" },
            { "<b>Socket:</b>", "<b>Сокет:</b>" },
            
            // Видеокарта
            { "<b>Hashrate:</b>", "<b>Хешрейт:</b>" },
            
            // Память
            { "<b>Memory:</b>", "<b>Память:</b>" },
            { "<b>Model:</b>", "<b>Модель:</b>" },
            
            // Материнская плата
            { "<b>CPUs:</b>", "<b>Процессоры:</b>" },
            { "<b>RAMs:</b>", "<b>Память:</b>" },
            { "<b>Max socket RAM:</b>", "<b>Макс. RAM на сокет:</b>" },
            { "<b>PCIs:</b>", "<b>PCI слоты:</b>" },
            
            // Блок питания
            { "<b>Power:</b>", "<b>Мощность:</b>" },
            
            // Сетевые карты
            { "<b>Monitor support:</b>", "<b>Поддержка мониторинга:</b>" },
            { "<b>Ethernet card</b>", "<b>Ethernet карта</b>" },
            { "<b>WiFi card</b>", "<b>WiFi карта</b>" },
            
            // Прочее
            { " Yes", " Да" },
            { " No", " Нет" }
        };

        /// <summary>
        /// Инициализация модуля ShopPatch
        /// </summary>
        public static void Initialize()
        {
            GreyHackRussianPlugin.Log.LogInfo("Инициализация ShopPatch...");

            // Обеспечить существование директорий
            EnsureDirectoriesExist();

            // Инициализация XML файла с непереведенными текстами
            InitializeXmlFile();

            try
            {
                // Регистрация патчей для конкретных методов
                RegisterPatches();
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при инициализации ShopPatch: {ex.Message}");
                GreyHackRussianPlugin.Log.LogDebug($"Stack trace: {ex.StackTrace}");
            }

            GreyHackRussianPlugin.Log.LogInfo("ShopPatch успешно инициализирован");
        }

        /// <summary>
        /// Регистрирует патчи для конкретных методов
        /// </summary>
        private static void RegisterPatches()
        {
            // Здесь мы не делаем ручную регистрацию, так как используем атрибуты HarmonyPatch
            GreyHackRussianPlugin.Log.LogInfo("Патчи для магазина будут применены через атрибуты Harmony");
        }

        /// <summary>
        /// Проверка и создание необходимых директорий
        /// </summary>
        private static void EnsureDirectoriesExist()
        {
            try
            {
                string shopPatchDir = Path.Combine(GreyHackRussianPlugin.PluginPath, "ShopPatch");
                if (!Directory.Exists(shopPatchDir))
                {
                    Directory.CreateDirectory(shopPatchDir);
                    GreyHackRussianPlugin.Log.LogInfo($"Создана директория для ShopPatch: {shopPatchDir}");
                }
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при создании директорий: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализация XML-файла для непереведенных текстов
        /// </summary>
        private static void InitializeXmlFile()
        {
            try
            {
                if (!File.Exists(untranslatedXmlPath))
                {
                    XmlDocument xmlDoc = new XmlDocument();

                    // Добавляем комментарий с описанием файла
                    XmlComment comment = xmlDoc.CreateComment(@"
    Файл содержит непереведенные или частично переведенные тексты магазина.
    
    Структура файла:
    <untranslated_texts> - корневой элемент
        <statistics> - статистическая информация
            <total_texts> - общее количество обработанных текстов
            <fully_translated> - полностью переведенные тексты
            <partially_translated> - частично переведенные тексты
            <untranslated> - непереведенные тексты
            <coverage_percentage> - процент покрытия переводами
        </statistics>
        
        <items> - список непереведенных текстов
            <item id=""1"" timestamp=""2023-10-15T14:30:25"" quality=""0.2"" source=""ItemShop""> - элемент с непереведенным текстом
                <original> - оригинальный текст
                    Текст оригинала
                </original>
                <partial_translation> - частичный перевод (если есть)
                    Текст частичного перевода
                </partial_translation>
                <untranslated_terms> - непереведенные термины (если анализ доступен)
                    <term>термин 1</term>
                    <term>термин 2</term>
                </untranslated_terms>
            </item>
        </items>
    </untranslated_texts>
    ");

                    XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    xmlDoc.AppendChild(xmlDeclaration);
                    xmlDoc.AppendChild(comment);

                    XmlElement rootElement = xmlDoc.CreateElement("untranslated_texts");
                    xmlDoc.AppendChild(rootElement);

                    XmlElement statisticsElement = xmlDoc.CreateElement("statistics");
                    rootElement.AppendChild(statisticsElement);

                    AddStatElement(xmlDoc, statisticsElement, "total_texts", "0");
                    AddStatElement(xmlDoc, statisticsElement, "fully_translated", "0");
                    AddStatElement(xmlDoc, statisticsElement, "partially_translated", "0");
                    AddStatElement(xmlDoc, statisticsElement, "untranslated", "0");
                    AddStatElement(xmlDoc, statisticsElement, "coverage_percentage", "0%");

                    XmlElement itemsElement = xmlDoc.CreateElement("items");
                    rootElement.AppendChild(itemsElement);

                    xmlDoc.Save(untranslatedXmlPath);
                    GreyHackRussianPlugin.Log.LogInfo($"Создан XML-файл для непереведенных текстов: {untranslatedXmlPath}");
                }
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при инициализации XML-файла: {ex.Message}");
            }
        }

        /// <summary>
        /// Вспомогательный метод для добавления элемента статистики
        /// </summary>
        private static void AddStatElement(XmlDocument xmlDoc, XmlElement parent, string name, string value)
        {
            XmlElement element = xmlDoc.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
        }

        /// <summary>
        /// Сохраняет непереведенный текст в XML файл
        /// </summary>
        private static void SaveUntranslatedTextToXml(string original, string partialTranslation, float quality, string source)
        {
            try
            {
                // Проверяем существование директории
                string directory = Path.GetDirectoryName(untranslatedXmlPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Создаем файл, если не существует
                if (!File.Exists(untranslatedXmlPath))
                {
                    InitializeXmlFile();
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(untranslatedXmlPath);

                // Получаем корневой элемент
                XmlElement rootElement = xmlDoc.DocumentElement;

                // Получаем элемент items
                XmlElement itemsElement = (XmlElement)rootElement.SelectSingleNode("items");
                if (itemsElement == null)
                {
                    itemsElement = xmlDoc.CreateElement("items");
                    rootElement.AppendChild(itemsElement);
                }

                // Проверяем, существует ли уже этот текст
                XmlNodeList existingItems = itemsElement.SelectNodes($"item/original[text()='{EscapeXml(original)}']");
                if (existingItems.Count > 0)
                {
                    // Текст уже существует, обновляем его
                    foreach (XmlNode existingItem in existingItems)
                    {
                        XmlElement itemElement = (XmlElement)existingItem.ParentNode;
                        itemElement.SetAttribute("timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                        itemElement.SetAttribute("quality", quality.ToString("0.00"));

                        XmlElement translationElement = (XmlElement)itemElement.SelectSingleNode("partial_translation");
                        if (translationElement == null)
                        {
                            translationElement = xmlDoc.CreateElement("partial_translation");
                            itemElement.AppendChild(translationElement);
                        }
                        translationElement.InnerText = partialTranslation;

                        // Получаем непереведенные термины
                        List<string> untranslatedTerms = GetUntranslatedTerms(original, partialTranslation);

                        // Обновляем список непереведенных терминов
                        XmlElement termsElement = (XmlElement)itemElement.SelectSingleNode("untranslated_terms");
                        if (termsElement == null)
                        {
                            termsElement = xmlDoc.CreateElement("untranslated_terms");
                            itemElement.AppendChild(termsElement);
                        }
                        else
                        {
                            termsElement.InnerXml = ""; // Очищаем существующие термины
                        }

                        // Добавляем новые термины
                        foreach (string term in untranslatedTerms)
                        {
                            XmlElement termElement = xmlDoc.CreateElement("term");
                            termElement.InnerText = term;
                            termsElement.AppendChild(termElement);
                        }
                    }
                }
                else
                {
                    // Создаем новый элемент item
                    XmlElement itemElement = xmlDoc.CreateElement("item");
                    itemElement.SetAttribute("id", (itemsElement.ChildNodes.Count + 1).ToString());
                    itemElement.SetAttribute("timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    itemElement.SetAttribute("quality", quality.ToString("0.00"));
                    itemElement.SetAttribute("source", source);

                    // Добавляем оригинальный текст
                    XmlElement originalElement = xmlDoc.CreateElement("original");
                    originalElement.InnerText = original;
                    itemElement.AppendChild(originalElement);

                    // Добавляем частичный перевод
                    XmlElement translationElement = xmlDoc.CreateElement("partial_translation");
                    translationElement.InnerText = partialTranslation;
                    itemElement.AppendChild(translationElement);

                    // Получаем непереведенные термины
                    List<string> untranslatedTerms = GetUntranslatedTerms(original, partialTranslation);

                    // Добавляем список непереведенных терминов
                    if (untranslatedTerms.Count > 0)
                    {
                        XmlElement termsElement = xmlDoc.CreateElement("untranslated_terms");
                        itemElement.AppendChild(termsElement);

                        foreach (string term in untranslatedTerms)
                        {
                            XmlElement termElement = xmlDoc.CreateElement("term");
                            termElement.InnerText = term;
                            termsElement.AppendChild(termElement);
                        }
                    }

                    // Добавляем элемент в список
                    itemsElement.AppendChild(itemElement);
                }

                // Обновляем статистику
                UpdateStatistics(xmlDoc);

                // Сохраняем файл
                xmlDoc.Save(untranslatedXmlPath);

                GreyHackRussianPlugin.Log.LogInfo($"Сохранен непереведенный текст из источника {source} в XML файл (качество перевода: {quality:P0})");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при сохранении непереведенного текста в XML: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает список непереведенных терминов путем сравнения оригинального и переведенного текста
        /// </summary>
        private static List<string> GetUntranslatedTerms(string original, string translation)
        {
            List<string> untranslatedTerms = new List<string>();

            // Делим на слова
            string[] originalWords = original.Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '(', ')', '[', ']', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);

            // Формируем потенциальные термины (слова и словосочетания длиной до 3 слов)
            for (int i = 0; i < originalWords.Length; i++)
            {
                string word = originalWords[i].Trim().ToLower();

                // Пропускаем игнорируемые слова и короткие слова
                if (ignoredWords.Contains(word) || word.Length < 4)
                    continue;

                // Проверяем термин длиной 1 слово
                if (!translation.ToLower().Contains(word) && !untranslatedTerms.Contains(word))
                {
                    untranslatedTerms.Add(originalWords[i]);
                }

                // Проверяем термины длиной 2-3 слова
                for (int j = 1; j <= 2 && i + j < originalWords.Length; j++)
                {
                    string phrase = string.Join(" ", originalWords, i, j + 1);
                    if (!translation.ToLower().Contains(phrase.ToLower()) && !untranslatedTerms.Contains(phrase))
                    {
                        untranslatedTerms.Add(phrase);
                    }
                }
            }

            return untranslatedTerms;
        }

        /// <summary>
        /// Экранирует специальные символы XML
        /// </summary>
        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        /// <summary>
        /// Обновляет статистику в XML файле
        /// </summary>
        private static void UpdateStatistics(XmlDocument xmlDoc)
        {
            try
            {
                XmlElement statisticsElement = (XmlElement)xmlDoc.DocumentElement.SelectSingleNode("statistics");
                if (statisticsElement == null)
                {
                    statisticsElement = xmlDoc.CreateElement("statistics");
                    xmlDoc.DocumentElement.PrependChild(statisticsElement);
                }

                // Подсчитываем статистику
                int totalTexts = translatedCount + untranslatedCount;
                int fullyTranslated = translatedCount;
                int partiallyTranslated = 0;
                int untranslated = 0;

                // Подсчитываем частично переведенные и непереведенные тексты
                XmlNodeList items = xmlDoc.DocumentElement.SelectNodes("items/item");
                foreach (XmlNode item in items)
                {
                    float quality;
                    if (float.TryParse(((XmlElement)item).GetAttribute("quality"), out quality))
                    {
                        if (quality <= 0.1f)
                            untranslated++;
                        else
                            partiallyTranslated++;
                    }
                }

                // Рассчитываем процент покрытия
                float coveragePercentage = totalTexts > 0 ? (float)fullyTranslated / totalTexts : 0;

                // Обновляем элементы статистики
                UpdateStatElement(statisticsElement, "total_texts", totalTexts.ToString());
                UpdateStatElement(statisticsElement, "fully_translated", fullyTranslated.ToString());
                UpdateStatElement(statisticsElement, "partially_translated", partiallyTranslated.ToString());
                UpdateStatElement(statisticsElement, "untranslated", untranslated.ToString());
                UpdateStatElement(statisticsElement, "coverage_percentage", $"{coveragePercentage:P0}");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при обновлении статистики: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет элемент статистики
        /// </summary>
        private static void UpdateStatElement(XmlElement statisticsElement, string name, string value)
        {
            XmlElement element = (XmlElement)statisticsElement.SelectSingleNode(name);
            if (element == null)
            {
                element = statisticsElement.OwnerDocument.CreateElement(name);
                statisticsElement.AppendChild(element);
            }
            element.InnerText = value;
        }

        /// <summary>
        /// Проверяет, является ли текст непереведенным на основе порога перевода
        /// </summary>
        private static bool IsUntranslated(string original, string translated)
        {
            if (original == translated) return true;

            // Делим на слова и считаем переведенные
            string[] originalWords = original.Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '(', ')', '[', ']', '<', '>' },
                StringSplitOptions.RemoveEmptyEntries);

            string[] translatedWords = translated.Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '(', ')', '[', ']', '<', '>' },
                StringSplitOptions.RemoveEmptyEntries);

            // Исключаем игнорируемые слова
            int originalCount = 0;
            foreach (var word in originalWords)
            {
                if (!ignoredWords.Contains(word.Trim().ToLower()))
                    originalCount++;
            }

            if (originalCount == 0) return false; // Все слова игнорируемые

            // Считаем процент переведенных слов (если совпадают оригинал и перевод, слово не переведено)
            float translationPercent = 1.0f - (float)Math.Min(originalCount, translatedWords.Length) / (float)originalCount;

            return translationPercent < translationThreshold;
        }

        /// <summary>
        /// Интеллектуальный перевод по частям с сохранением форматирования
        /// </summary>
        private static string TranslateByParts(string original)
        {
            string result = original;

            // Сохраняем HTML теги, заменяя их на временные метки
            Dictionary<string, string> htmlTags = new Dictionary<string, string>();
            int tagIndex = 0;

            // Регулярное выражение для поиска HTML тегов
            var tagRegex = new Regex(@"<[^>]+>");
            foreach (Match match in tagRegex.Matches(result))
            {
                string tag = match.Value;
                string placeholder = $"__TAG_{tagIndex}__";
                htmlTags[placeholder] = tag;
                result = result.Replace(tag, placeholder);
                tagIndex++;
            }

            // 1. Проверяем полные блоки текста через словарь переводов
            result = Translation.Translator.TranslateTextIgnoreCase(result);

            // 2. Проверяем HTML шаблоны с характеристиками
            foreach (var pattern in htmlPatterns)
            {
                if (result.Contains(pattern.Key))
                {
                    result = result.Replace(pattern.Key, pattern.Value);
                }
            }

            // 3. Заменяем общие фразы
            foreach (var phrase in commonPhrases)
            {
                if (result.Contains(phrase.Key))
                {
                    result = result.Replace(phrase.Key, phrase.Value);
                }
            }

            // Восстанавливаем HTML теги
            foreach (var tag in htmlTags)
            {
                result = result.Replace(tag.Key, tag.Value);
            }

            return result;
        }

        /// <summary>
        /// Рассчитывает примерное качество перевода (от 0.0 до 1.0)
        /// </summary>
        private static float CalculateTranslationQuality(string original, string translated)
        {
            // Делим на слова
            string[] originalWords = original.Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '(', ')', '[', ']', '<', '>' },
                StringSplitOptions.RemoveEmptyEntries);

            string[] translatedWords = translated.Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '(', ')', '[', ']', '<', '>' },
                StringSplitOptions.RemoveEmptyEntries);

            // Исключаем игнорируемые слова
            int originalCount = 0;
            int sameWordsCount = 0;

            // Подсчитываем количество значимых слов в оригинале
            foreach (var word in originalWords)
            {
                string trimmedWord = word.Trim().ToLower();
                if (!ignoredWords.Contains(trimmedWord) && trimmedWord.Length >= 3)
                {
                    originalCount++;

                    // Проверяем, есть ли это слово в переводе
                    if (translated.ToLower().Contains(trimmedWord))
                    {
                        sameWordsCount++;
                    }
                }
            }

            if (originalCount == 0) return 1.0f; // Все слова игнорируемые или очень короткие

            // Возвращаем долю переведенных слов
            return 1.0f - ((float)sameWordsCount / (float)originalCount);
        }

        /// <summary>
        /// Обрабатывает перевод текста и записывает результаты
        /// </summary>
        private static string ProcessTranslation(string original, string source)
        {
            // Проверка на пустую строку
            if (string.IsNullOrEmpty(original))
                return original;

            // Проверяем, был ли этот текст уже переведен ранее
            if (translationCache.TryGetValue(original, out string cachedTranslation))
            {
                return cachedTranslation;
            }

            // Многоуровневый подход к переводу
            string translated = original;

            // Переводим блоками с сохранением форматирования
            translated = TranslateByParts(original);

            // Если произошел перевод (хотя бы частичный)
            if (translated != original)
            {
                // Проверяем качество перевода
                bool isNotFullyTranslated = IsUntranslated(original, translated);

                if (isNotFullyTranslated)
                {
                    // Текст переведен частично - сохраняем в XML для анализа
                    GreyHackRussianPlugin.Log.LogInfo($"[!] Перевод неполный, качество ниже порога ({translationThreshold * 100}%)");

                    // Вычисляем примерное качество перевода
                    float translationQuality = CalculateTranslationQuality(original, translated);

                    // Сохраняем в XML только если качество ниже порогового значения
                    SaveUntranslatedTextToXml(original, translated, translationQuality, source);
                }

                // Увеличиваем счетчик
                translatedCount++;

                // Добавляем в кэш для повторного использования
                translationCache[original] = translated;

                // Логирование (ограниченное)
                if (translatedCount % 10 == 0 || original.Length > 50)
                {
                    int previewLength = Math.Min(50, original.Length);
                    string originalPreview = original.Substring(0, previewLength) + (original.Length > previewLength ? "..." : "");
                    string translatedPreview = translated.Substring(0, Math.Min(50, translated.Length)) + (translated.Length > previewLength ? "..." : "");

                    GreyHackRussianPlugin.Log.LogInfo($"[{source}] Перевод #{translatedCount}: '{originalPreview}' -> '{translatedPreview}'");
                }
            }
            else
            {
                // Текст не переведен совсем
                untranslatedCount++;

                // Сохраняем в XML для последующего анализа
                SaveUntranslatedTextToXml(original, original, 0.0f, source);

                // Вывод в лог (ограниченный)
                if (original.Length > 10 && untranslatedCount % 5 == 0)
                {
                    GreyHackRussianPlugin.Log.LogInfo($"[{source}] Не найден перевод: '{original}'");
                }
            }

            return translated;
        }

        // -----------------------------------------------------------------
        // ПАТЧИ ДЛЯ МЕТОДОВ КОНФИГУРАЦИИ МАГАЗИНОВ
        // -----------------------------------------------------------------

        /// <summary>
        /// Патч для базового метода Configure в ItemShop
        /// </summary>
        [HarmonyPatch(typeof(ItemShop), "Configure", new Type[] { typeof(string), typeof(string) })]
        public static class ItemShopConfigurePatch
        {
            static void Prefix(ref string nombreItem, ref string infoProgram)
            {
                try
                {
                    // Переводим название и описание
                    nombreItem = ProcessTranslation(nombreItem, "ItemShop.Configure.Name");
                    infoProgram = ProcessTranslation(infoProgram, "ItemShop.Configure.Info");
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в ItemShopConfigurePatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Патч для метода Configure в ItemShopHardware
        /// </summary>
        [HarmonyPatch(typeof(ItemShopHardware), "Configure")]
        public static class ItemShopHardwareConfigurePatch
        {
            static void Prefix(Hardware.ItemHardware itemHardware, ref HtmlBrowser browser)
            {
                try
                {
                    // Получаем описание из itemHardware и переводим его
                    if (itemHardware != null)
                    {
                        string description = itemHardware.GetDescripcionTienda();
                        if (!string.IsNullOrEmpty(description))
                        {
                            // Переводим описание
                            string translated = ProcessTranslation(description, "ItemShopHardware.Description");

                            // Если удалось перевести, подменяем через рефлексию
                            if (translated != description)
                            {
                                // Метод GetDescripcionTienda просто возвращает поле, поэтому нам нужно найти поле с описанием
                                // Это сложный случай, требующий использования рефлексии для доступа к защищенным полям
                                // В реальном плагине это может потребовать дополнительной разработки
                                GreyHackRussianPlugin.Log.LogInfo($"Переведено описание аппаратного обеспечения");
                            }
                        }

                        // Переводим имя, если оно задано
                        if (!string.IsNullOrEmpty(itemHardware.name))
                        {
                            itemHardware.name = ProcessTranslation(itemHardware.name, "ItemShopHardware.Name");
                        }
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в ItemShopHardwareConfigurePatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Патч для метода OnBuy базового класса ItemShop
        /// </summary>
        [HarmonyPatch(typeof(ItemShop), "OnBuy")]
        public static class ItemShopOnBuyPatch
        {
            static void Prefix(ItemShop __instance)
            {
                try
                {
                    // Патчим установку текста заголовка окна
                    FieldInfo prefabPreBuyField = AccessTools.Field(typeof(ItemShop), "prefabPreBuyObj");
                    GameObject prefabPreBuy = (GameObject)prefabPreBuyField.GetValue(__instance);

                    if (prefabPreBuy != null)
                    {
                        // Ничего не делаем здесь, так как мы перехватим установку заголовка в другом месте
                        GreyHackRussianPlugin.Log.LogInfo("Перехвачен вызов OnBuy для ItemShop");
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в ItemShopOnBuyPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Патч для перехвата установки текста заголовка в диалоговом окне (для метода SetTitleText)
        /// </summary>
        [HarmonyPatch(typeof(uDialog), "SetTitleText")]
        public static class DialogTitleTextPatch
        {
            static void Prefix(ref string titleText, bool localize)
            {
                try
                {
                    if (!string.IsNullOrEmpty(titleText))
                    {
                        titleText = ProcessTranslation(titleText, "uDialog.Title");
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в DialogTitleTextPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Патч для метода OnBuy в ItemShopHardware
        /// </summary>
        [HarmonyPatch(typeof(ItemShopHardware), "OnBuy")]
        public static class ItemShopHardwareOnBuyPatch
        {
            static void Prefix(ItemShopHardware __instance)
            {
                try
                {
                    // Используем рефлексию для доступа к защищенному полю browser
                    FieldInfo browserField = AccessTools.Field(typeof(ItemShop), "browser");
                    HtmlBrowser browser = (HtmlBrowser)browserField.GetValue(__instance);

                    if (browser == null)
                    {
                        GreyHackRussianPlugin.Log.LogInfo("Перехвачен вызов OnBuy для ItemShopHardware без браузера");
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в ItemShopHardwareOnBuyPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Патч для метода ShowQuestionWindow в OS
        /// </summary>
        [HarmonyPatch(typeof(OS), "ShowQuestionWindow")]
        public static class ShowQuestionWindowPatch
        {
            static void Prefix(ref string message, object listener, ref string positiveText, ref string negativeText)
            {
                try
                {
                    // Переводим сообщение и тексты кнопок
                    message = ProcessTranslation(message, "OS.QuestionWindow.Message");
                    positiveText = ProcessTranslation(positiveText, "OS.QuestionWindow.Positive");
                    negativeText = ProcessTranslation(negativeText, "OS.QuestionWindow.Negative");
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в ShowQuestionWindowPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Патч для перевода текстовых компонентов TMP_Text (используется в ценах и метках)
        /// </summary>
        [HarmonyPatch(typeof(TMP_Text), "set_text")]
        public static class TMPTextPatch
        {
            // Кэш для предотвращения повторных переводов одного и того же текста
            private static readonly HashSet<int> processedTexts = new HashSet<int>();

            static void Prefix(ref string value, TMP_Text __instance)
            {
                if (string.IsNullOrEmpty(value)) return;

                try
                {
                    // Определяем, является ли это текстом для магазина
                    bool isShopText = IsShopText(__instance);

                    if (isShopText && !value.StartsWith("$") && value != "Yes" && value != "No")
                    {
                        int hash = value.GetHashCode();

                        // Проверяем, обрабатывали ли мы уже этот текст
                        if (!processedTexts.Contains(hash))
                        {
                            string original = value;
                            value = ProcessTranslation(value, "TMP_Text.Shop");

                            if (value != original)
                            {
                                processedTexts.Add(hash);
                                GreyHackRussianPlugin.Log.LogInfo($"TMP_Text переведен: '{original}' -> '{value}' на объекте '{__instance?.gameObject?.name}'");
                            }

                            // Ограничиваем размер кэша
                            if (processedTexts.Count > 1000)
                            {
                                processedTexts.Clear();
                            }
                        }
                    }
                    // Обрабатываем Yes/No для кнопок
                    else if (isShopText && (value == "Yes" || value == "No"))
                    {
                        value = ProcessTranslation(value, "TMP_Text.Button");
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка в TMPTextPatch: {ex.Message}");
                }
            }

            /// <summary>
            /// Определяет, относится ли текстовый компонент к магазину
            /// </summary>
            private static bool IsShopText(TMP_Text text)
            {
                if (text == null) return false;

                // Проверяем иерархию объектов для определения контекста магазина
                Transform parent = text.transform.parent;
                while (parent != null)
                {
                    string objName = parent.name.ToLower();
                    if (objName.Contains("shop") || objName.Contains("store") ||
                        objName.Contains("item") || objName.Contains("buy") ||
                        objName.Contains("sell") || objName.Contains("dialog"))
                    {
                        return true;
                    }
                    parent = parent.parent;
                }

                // Проверяем, привязан ли компонент к одному из классов магазина
                var itemShop = text.GetComponentInParent<ItemShop>();
                if (itemShop != null) return true;

                return false;
            }
        }
    }
}