using GreyHackRussianPlugin.Translation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using TMPro;
using UnityEngine;

namespace GreyHackRussianPlugin.Patches
{
    /// <summary>
    /// Объединенный патч для перевода текстов в магазине игры
    /// </summary>
    [HarmonyPatch]
    public class ShopPatch
    {
        // Словарь для кэширования переведенных описаний
        private static readonly Dictionary<string, string> translationCache = new Dictionary<string, string>();

        // Счетчик переведенных описаний
        private static int translatedCount = 0;

        // Счетчик непереведенных описаний
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
            "mb", "gb", "rpm", "mhz", "ghz", "quality", "size", "speed",
            "power", "model", "memory", "cores", "socket", "hashrate", "mh/s",
            "cpus", "rams", "pci", "kga", "sga", "nga", "ddr1", "ddr2", "ddr3",
            "ethernet", "wifi", "monitor", "support", "yes", "no", "/10"
        };

        // Список общих фраз для замены
        private static readonly Dictionary<string, string> commonPhrases = new Dictionary<string, string>
        {
            { "Scan remote ips to find open ports.", "Сканирование удаленных IP для поиска открытых портов." },
            { "Displays the user accounts registered on the server where the SMTP service is running.", "Отображает учетные записи пользователей, зарегистрированных на сервере с запущенной службой SMTP." },
            { "Install an SSH access server on the machine.", "Установить SSH сервер на компьютер." },
            { "Install an HTTP server on the machine to host web pages.", "Установить HTTP сервер на компьютер для размещения веб-страниц." },
            { "Install an FTP access server on the machine.", "Установить FTP сервер на компьютер." },
            { "Install a chat server that can be configured as public or private.", "Установить чат-сервер, который можно настроить как публичный или приватный." },
            { "Install a repository server to host different programs.", "Установить репозиторий для хранения различных программ." },
            { "Program to manage the wallet in text mode to be used from the Terminal", "Программа для управления кошельком в текстовом режиме, используемая из Терминала." },
            { "Program that displays information on all published coins as well as pending offers.", "Программа, отображающая информацию обо всех опубликованных монетах, а также о ожидающих предложениях." },
            { "Tool to report vulnerabilities in system libraries.", "Инструмент для сообщения об уязвимостях в системных библиотеках." },
            { "Yes", "Да" },
            { "No", "Нет" }
        };

        // ОБЪЕДИНЕННЫЙ словарь шаблонов для замены HTML-форматирования и технических характеристик
        private static readonly Dictionary<string, string> htmlPatterns = new Dictionary<string, string>
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

        // Инициализация - должна вызываться при старте плагина
        public static void Initialize()
        {
            GreyHackRussianPlugin.Log.LogInfo("Инициализация объединенного ShopPatch...");

            // Обеспечить существование директорий
            EnsureDirectoriesExist();

            // Инициализация XML файла с непереведенными текстами
            InitializeXmlFile();

            try
            {
                // УЛУЧШЕННОЕ ЛОГИРОВАНИЕ: выводим все найденные классы и методы, относящиеся к магазину
                LogClassInfo("ItemShop");
                LogClassInfo("ItemShopAdvanced");
                LogClassInfo("ItemShopHardware");
                LogClassInfo("PreBuy");
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при логировании информации о классах: {ex.Message}");
            }

            GreyHackRussianPlugin.Log.LogInfo("Объединенный ShopPatch успешно инициализирован");
        }

        /// <summary>
        /// Логирует информацию о классе и его методах
        /// </summary>
        private static void LogClassInfo(string className)
        {
            try
            {
                Type type = AccessTools.TypeByName(className);

                if (type == null)
                {
                    GreyHackRussianPlugin.Log.LogWarning($"Класс {className} не найден");
                    return;
                }

                GreyHackRussianPlugin.Log.LogInfo($"Найден класс {className}");

                // Получаем и выводим все публичные методы
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    string paramInfo = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                    GreyHackRussianPlugin.Log.LogInfo($"  Метод: {method.ReturnType.Name} {method.Name}({paramInfo})");
                }

                // Получаем и выводим все публичные поля
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    GreyHackRussianPlugin.Log.LogInfo($"  Поле: {field.FieldType.Name} {field.Name}");
                }
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"Ошибка при получении информации о классе {className}: {ex.Message}");
            }
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
                    GreyHackRussianPlugin.Log.LogInfo($"Создан XML-файл для непереведенных текстов магазина: {untranslatedXmlPath}");
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
        /// Интеллектуальный перевод текста с использованием различных стратегий
        /// </summary>
        private static string TranslateShopText(string original)
        {
            // Проверка кэша
            if (translationCache.TryGetValue(original, out string cachedTranslation))
            {
                GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Используется кешированный перевод");
                return cachedTranslation;
            }

            string result = original;

            // 1. Сначала пытаемся найти точный перевод
            result = Translation.Translator.TranslateText(original);

            // Если найден точный перевод, логируем это
            if (result != original)
            {
                GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Найден точный перевод в словаре");
            }

            // 2. Если точного перевода нет, пытаемся применить общие фразы
            if (result == original)
            {
                GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Точный перевод не найден, применяем шаблоны замены");

                // Сохраняем оригинал для сравнения
                string beforeTemplates = result;

                // Применяем общие фразы
                foreach (var phrase in commonPhrases)
                {
                    if (result.Contains(phrase.Key))
                    {
                        result = result.Replace(phrase.Key, phrase.Value);
                    }
                }

                // Применяем HTML паттерны для технических характеристик
                foreach (var pattern in htmlPatterns)
                {
                    if (result.Contains(pattern.Key))
                    {
                        result = result.Replace(pattern.Key, pattern.Value);
                    }
                }

                // Проверяем, был ли текст изменен шаблонами
                if (result != beforeTemplates)
                {
                    GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Применены шаблоны замены");
                }
            }

            // Если перевод получился, кэшируем его
            if (result != original)
            {
                translationCache[original] = result;
                GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Перевод сохранен в кеш");
            }
            else
            {
                GreyHackRussianPlugin.Log.LogWarning("ShopPatch: Текст не был переведен");
            }

            return result;
        }

        /// <summary>
        /// Общий метод для перевода HTML-контента
        /// </summary>
        private static void TranslateHtml(ref string html, string source)
        {
            if (string.IsNullOrEmpty(html))
            {
                GreyHackRussianPlugin.Log.LogWarning($"ShopPatch: Получен пустой HTML от {source}");
                return;
            }

            // Логируем начало обработки HTML
            string shortHtml = html.Length > 50 ? html.Substring(0, 50) + "..." : html;
            GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: Начата обработка HTML от {source}: {shortHtml}");

            string originalHtml = html;

            // 1. Сначала пытаемся найти точный перевод
            string translated = Translation.Translator.TranslateText(html);
            if (translated != html)
            {
                html = translated;
                GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: HTML успешно переведен через словарь");
                return;
            }

            // 2. Если точного перевода нет, применяем шаблоны замен
            foreach (var replacement in htmlPatterns)
            {
                html = html.Replace(replacement.Key, replacement.Value);
            }

            // Если что-то изменилось, логируем это
            if (html != originalHtml)
            {
                GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: HTML-контент успешно переведен через шаблоны замен");
            }
            else
            {
                // Сохраняем непереведенный контент для анализа
                float quality = CalculateTranslationQuality(html, html);
                SaveUntranslatedTextToXml(html, html, quality, $"HTML-{source}");
                GreyHackRussianPlugin.Log.LogWarning($"ShopPatch: HTML-контент не был переведен, сохранен для анализа");
            }
        }

        // Патч для ItemShop.Configure(string, string)
        [HarmonyPatch(typeof(ItemShop), "Configure", new Type[] {
            typeof(string),
            typeof(string)
        })]
        [HarmonyPostfix]
        public static void ItemShopPostfix(ItemShop __instance, string nombreItem, string infoProgram)
        {
            try
            {
                // Логируем для отладки с ограничением длины текста
                string logText = infoProgram;
                if (logText.Length > 50)
                    logText = logText.Substring(0, 47) + "...";

                GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: обработка ItemShop текста: {logText}");

                // Получаем поле description через reflection
                FieldInfo field = typeof(ItemShop).GetField("description", BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    TMP_Text description = field.GetValue(__instance) as TMP_Text;
                    if (description != null && !string.IsNullOrEmpty(infoProgram))
                    {
                        // Увеличиваем счетчик обработанных текстов
                        untranslatedCount++;

                        // Переводим текст с использованием расширенных возможностей
                        string translated = TranslateShopText(infoProgram);

                        // Если текст был переведен (не равен оригиналу)
                        if (translated != infoProgram)
                        {
                            // Увеличиваем счетчик переведенных текстов
                            translatedCount++;

                            // Проверяем качество перевода
                            bool isPartiallyTranslated = IsUntranslated(infoProgram, translated);

                            // Если перевод неполный, сохраняем для дальнейшего анализа
                            if (isPartiallyTranslated)
                            {
                                // Вычисляем качество перевода
                                float quality = CalculateTranslationQuality(infoProgram, translated);

                                // Сохраняем с информацией о качестве перевода
                                SaveUntranslatedTextToXml(infoProgram, translated, quality, "ItemShop");
                            }
                        }
                        else
                        {
                            // Текст не переведен совсем, сохраняем для перевода
                            SaveUntranslatedTextToXml(infoProgram, "", 0.0f, "ItemShop");
                        }

                        // В любом случае применяем перевод (даже если он равен оригиналу)
                        description.text = translated;
                        GreyHackRussianPlugin.Log.LogInfo("ShopPatch: применен перевод для ItemShop.description");
                    }
                }
                else
                {
                    GreyHackRussianPlugin.Log.LogError("ShopPatch: поле description не найдено");

                    // Диагностика - выводим все поля класса
                    FieldInfo[] fields = typeof(ItemShop).GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var f in fields)
                    {
                        GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: найдено поле {f.Name} типа {f.FieldType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                GreyHackRussianPlugin.Log.LogError($"ShopPatch: ошибка в патче ItemShop: {ex.Message}");
                // Расширенная информация об ошибке
                GreyHackRussianPlugin.Log.LogError($"Стек вызова: {ex.StackTrace}");
            }
        }

        // Патч для PreBuy.Configure
        [HarmonyPatch]
        public class PreBuyPatch
        {
            static MethodBase TargetMethod()
            {
                try
                {
                    // Ищем метод Configure в классе PreBuy с 8 параметрами
                    Type preBuyType = typeof(PreBuy);

                    GreyHackRussianPlugin.Log.LogInfo($"ShopPatch.PreBuyPatch: Поиск метода Configure в классе {preBuyType.FullName}");

                    foreach (var method in preBuyType.GetMethods())
                    {
                        if (method.Name == "Configure")
                        {
                            var parameters = method.GetParameters();
                            if (parameters.Length == 8)
                            {
                                GreyHackRussianPlugin.Log.LogInfo($"ShopPatch.PreBuyPatch: Найден метод Configure с 8 параметрами");
                                return method;
                            }
                            else
                            {
                                GreyHackRussianPlugin.Log.LogInfo($"ShopPatch.PreBuyPatch: Найден метод Configure с {parameters.Length} параметрами");
                            }
                        }
                    }

                    GreyHackRussianPlugin.Log.LogWarning("ShopPatch.PreBuyPatch: Метод Configure с 8 параметрами не найден");
                    return null;
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"ShopPatch.PreBuyPatch: Ошибка при поиске метода Configure: {ex.Message}");
                    return null;
                }
            }

            static void Postfix(PreBuy __instance, string nombreItemShop, string description)
            {
                try
                {
                    // Логируем для отладки с ограничением длины текста
                    string logText = description;
                    if (logText.Length > 50)
                        logText = logText.Substring(0, 47) + "...";

                    GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: обработка PreBuy текста: {logText}");

                    // Получаем поле description через reflection
                    FieldInfo field = typeof(PreBuy).GetField("description", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        TMP_Text descriptionField = field.GetValue(__instance) as TMP_Text;
                        if (descriptionField != null && !string.IsNullOrEmpty(description))
                        {
                            // Увеличиваем счетчик обработанных текстов
                            untranslatedCount++;

                            // Переводим текст с использованием расширенных возможностей
                            string translated = TranslateShopText(description);

                            // Если текст был переведен (не равен оригиналу)
                            if (translated != description)
                            {
                                // Увеличиваем счетчик переведенных текстов
                                translatedCount++;

                                // Проверяем качество перевода
                                bool isPartiallyTranslated = IsUntranslated(description, translated);

                                // Если перевод неполный, сохраняем для дальнейшего анализа
                                if (isPartiallyTranslated)
                                {
                                    // Вычисляем качество перевода
                                    float quality = CalculateTranslationQuality(description, translated);

                                    // Сохраняем с информацией о качестве перевода
                                    SaveUntranslatedTextToXml(description, translated, quality, "PreBuy");
                                }
                            }
                            else
                            {
                                // Текст не переведен совсем, сохраняем для перевода
                                SaveUntranslatedTextToXml(description, "", 0.0f, "PreBuy");
                            }

                            // В любом случае применяем перевод (даже если он равен оригиналу)
                            descriptionField.text = translated;
                            GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Применен перевод для PreBuy.description");
                        }
                    }
                    else
                    {
                        GreyHackRussianPlugin.Log.LogError("ShopPatch: поле description в PreBuy не найдено");

                        // Диагностика - выводим все поля класса
                        FieldInfo[] fields = typeof(PreBuy).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var f in fields)
                        {
                            GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: найдено поле {f.Name} типа {f.FieldType.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"ShopPatch: ошибка в патче PreBuy: {ex.Message}");
                    GreyHackRussianPlugin.Log.LogError($"Стек вызова: {ex.StackTrace}");
                }
            }
        }

        // НОВЫЙ БЛОК: Объединенный патч для деталей товара
        // Обновленный класс ItemShopDetailsPatch с правильной обработкой ошибок
        [HarmonyPatch]
        public class ItemShopDetailsPatch
        {
            static MethodBase TargetMethod()
            {
                try
                {
                    GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Поиск методов для патча деталей товара...");

                    // Если методы не найдены, возвращаем null и логируем это
                    GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Детальные методы не найдены, пропускаем патч");
                    return null; // Важно - возврат null предотвращает применение патча
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"ShopPatch: Ошибка при поиске метода: {ex.Message}");
                    return null; // Возврат null при ошибке
                }
            }

            static void Postfix(ref string __result)
            {
                // Код будет выполнен только если метод найден
                try
                {
                    GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Перевод HTML-контента...");
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"ShopPatch: Ошибка при обработке HTML: {ex.Message}");
                }
            }
        }

        // ДОПОЛНИТЕЛЬНЫЙ ПАТЧ для отображения деталей через ItemShop
        [HarmonyPatch(typeof(ItemShop))]
        [HarmonyPatch("OnBuy")]
        public class ItemShopOnBuyPatch
        {
            static void Postfix(ItemShop __instance)
            {
                try
                {
                    GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Перехвачен метод ItemShop.OnBuy");

                    // Диагностика для нахождения поля с текстом
                    GreyHackRussianPlugin.Log.LogInfo("ShopPatch: Поиск всех текстовых полей в ItemShop...");

                    FieldInfo[] fields = typeof(ItemShop).GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(TMP_Text) || field.FieldType.IsSubclassOf(typeof(TMP_Text)))
                        {
                            TMP_Text textField = field.GetValue(__instance) as TMP_Text;
                            if (textField != null && !string.IsNullOrEmpty(textField.text))
                            {
                                string originalText = textField.text;
                                string translatedText = TranslateShopText(originalText);

                                if (translatedText != originalText)
                                {
                                    textField.text = translatedText;
                                    GreyHackRussianPlugin.Log.LogInfo($"ShopPatch: Переведен текст в поле {field.Name}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"ShopPatch: Ошибка при патче OnBuy: {ex.Message}");
                }
            }
        }
    }
}