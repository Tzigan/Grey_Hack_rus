using BepInEx.Logging;
using GreyHackRussianPlugin.DebugTools;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GreyHackRussianPlugin.PluginUpdater
{
    /// <summary>
    /// Модуль для проверки и установки обновлений без использования GitHub API
    /// </summary>
    public class UpdateModule
    {
        // URL для обновления - прямая ссылка на JSON с информацией
        private const string UPDATE_INFO_URL = "https://raw.githubusercontent.com/Tzigan/Grey_Hack_rus/1.1.4-test/version.json";

        // Константы для файлов
        private const string UPDATE_FILE_PREFIX = "GreyHackRussianPlugin_v";
        private const string UPDATE_FILE_EXTENSION = ".zip";

        // Кэширование
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static bool _updateAvailable = false;
        private static string _cachedLatestVersion = null;
        private static string _cachedDownloadUrl = null;

        // Конфигурация
        private readonly string _currentVersion;
        private readonly string _pluginDirectoryPath;
        private readonly ManualLogSource _logger;
        private readonly DebugLogger _debugLog;

        // Состояние
        private string _latestVersion;
        private string _downloadUrl;
        private GameObject _updateWindow;
        private bool _isChecking = false;
        private string _changelog;

        /// <summary>
        /// Создает новый экземпляр модуля обновлений
        /// </summary>
        public UpdateModule(
            string currentVersion,
            string pluginDirectoryPath,
            ManualLogSource logger,
            DebugLogger debugLogger = null)
        {
            _currentVersion = currentVersion;
            _pluginDirectoryPath = pluginDirectoryPath;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _debugLog = debugLogger;

            if (string.IsNullOrEmpty(currentVersion))
                throw new ArgumentNullException(nameof(currentVersion));

            if (string.IsNullOrEmpty(pluginDirectoryPath))
                throw new ArgumentNullException(nameof(pluginDirectoryPath));
        }

        /// <summary>
        /// Вспомогательный метод для логирования с поддержкой DebugLogger
        /// </summary>
        private void LogDebug(string message, DebugLogger.LogLevel level = DebugLogger.LogLevel.Info)
        {
            // Обычное логирование всегда
            switch (level)
            {
                case DebugLogger.LogLevel.Info:
                    _logger.LogInfo($"[UpdateModule] {message}");
                    break;
                case DebugLogger.LogLevel.Warning:
                    _logger.LogWarning($"[UpdateModule] {message}");
                    break;
                case DebugLogger.LogLevel.Error:
                    _logger.LogError($"[UpdateModule] {message}");
                    break;
                case DebugLogger.LogLevel.Debug:
                    _logger.LogDebug($"[UpdateModule] {message}");
                    break;
            }

            // Расширенное логирование, если доступен DebugLogger
            if (_debugLog != null)
            {
                _debugLog.Log($"[UpdateModule] {message}", level);
            }
        }

        /// <summary>
        /// Проверяет наличие обновлений и показывает окно, если обновление доступно
        /// </summary>
        public async Task CheckForUpdates()
        {
            if (_isChecking) return;
            _isChecking = true;

            LogDebug("======== НАЧАЛО ПРОВЕРКИ ОБНОВЛЕНИЙ ========");
            try
            {
                // Кэширование на 24 часа
                if ((DateTime.Now - _lastCheckTime).TotalHours < 24 && _cachedLatestVersion != null)
                {
                    LogDebug($"Используем кэшированный результат. Последняя проверка: {_lastCheckTime}");

                    if (_updateAvailable)
                    {
                        _latestVersion = _cachedLatestVersion;
                        _downloadUrl = _cachedDownloadUrl;
                        LogDebug($"Загружена версия {_latestVersion} из кэша, показываем окно обновления");
                        ShowUpdateWindow();
                    }
                    return;
                }

                LogDebug($"Проверка обновлений. Текущая версия: {_currentVersion}");

                bool hasUpdate = await CheckDirectUpdate();

                if (hasUpdate)
                {
                    LogDebug($"Доступно обновление: {_latestVersion}");
                    ShowUpdateWindow();
                }
                else
                {
                    LogDebug("Обновлений не найдено");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Ошибка проверки обновлений: {ex.Message}", DebugLogger.LogLevel.Error);
                if (_debugLog != null)
                {
                    _debugLog.LogStackTrace();
                }
            }
            finally
            {
                LogDebug("======== КОНЕЦ ПРОВЕРКИ ОБНОВЛЕНИЙ ========");
                _isChecking = false;
            }
        }

        private async Task<bool> CheckDirectUpdate()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "GreyHackRusPlugin");

                    // Загружаем JSON с информацией об обновлении
                    LogDebug($"Запрос к URL: {UPDATE_INFO_URL}");
                    string response = await client.DownloadStringTaskAsync(UPDATE_INFO_URL);
                    LogDebug("Информация об обновлении успешно загружена");

                    // Парсим JSON
                    LogDebug("Парсинг данных JSON...");
                    JObject updateInfo = JObject.Parse(response);
                    _latestVersion = updateInfo["version"]?.ToString();
                    _downloadUrl = updateInfo["download_url"]?.ToString();
                    _changelog = updateInfo["changelog"]?.ToString() ?? "Нет информации о изменениях";

                    // Проверяем, что необходимые данные получены
                    if (string.IsNullOrEmpty(_latestVersion) || string.IsNullOrEmpty(_downloadUrl))
                    {
                        LogDebug("Некорректный формат данных об обновлении", DebugLogger.LogLevel.Warning);
                        return false;
                    }

                    LogDebug($"Получена информация: версия {_latestVersion}, URL: {_downloadUrl}");
                    LogDebug($"Changelog: {_changelog}");

                    // Сравниваем версии
                    LogDebug($"Сравнение версий: текущая={_currentVersion}, новая={_latestVersion}");
                    Version current = new Version(_currentVersion);
                    Version latest = new Version(_latestVersion);

                    bool hasUpdate = latest > current;
                    LogDebug($"Результат сравнения: {(hasUpdate ? "Доступно обновление" : "Обновление не требуется")}");

                    // Кэшируем результаты проверки
                    _lastCheckTime = DateTime.Now;
                    _updateAvailable = hasUpdate;
                    _cachedLatestVersion = _latestVersion;
                    _cachedDownloadUrl = _downloadUrl;
                    LogDebug($"Данные кэшированы до {_lastCheckTime.AddHours(24)}");

                    return hasUpdate;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Ошибка при проверке обновлений: {ex.Message}", DebugLogger.LogLevel.Error);
                if (_debugLog != null)
                {
                    _debugLog.LogStackTrace();
                }
                return false;
            }
        }

        private void ShowUpdateWindow()
        {
            // Выполняем в основном потоке Unity
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                CreateUpdateWindow();
            });
        }

        private void CreateUpdateWindow()
        {
            LogDebug("Создание окна обновления в стиле Grey Hack");

            // Создаем окно с Canvas
            _updateWindow = new GameObject("UpdateWindow");
            GameObject.DontDestroyOnLoad(_updateWindow);

            Canvas canvas = _updateWindow.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Поверх всего UI

            CanvasScaler scaler = _updateWindow.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _updateWindow.AddComponent<GraphicRaycaster>();

            // Создаем затемненный фон в стиле хакерского интерфейса
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_updateWindow.transform, false);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); // Почти черный фон
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // Создаем панель в стиле терминала
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_updateWindow.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.09f, 0.95f); // Темно-серый фон

            // Добавляем рамку в хакерском стиле
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.8f, 0.4f, 0.8f); // Неоново-зеленая рамка
            outline.effectDistance = new Vector2(2, 2);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(580, 350);
            panelRect.anchoredPosition = Vector2.zero;

            // Заголовок в хакерском стиле
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(panel.transform, false);
            Image titleBarImage = titleBar.AddComponent<Image>();
            titleBarImage.color = new Color(0.0f, 0.5f, 0.2f, 0.8f); // Терминально-зеленый цвет
            RectTransform titleBarRect = titleBar.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1f);
            titleBarRect.sizeDelta = new Vector2(0, 30);
            titleBarRect.anchoredPosition = Vector2.zero;

            // Текст заголовка в стиле терминала
            CreateHackerText(titleBar, ":: SYSTEM UPDATE AVAILABLE ::", new Vector2(0, -15), 16,
                TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f));

            // Информация о версиях в терминальном стиле
            CreateHackerText(panel, $"> Current version: {_currentVersion}", new Vector2(-180, 60), 16,
                TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));
            CreateHackerText(panel, $"> New version: {_latestVersion}", new Vector2(-180, 30), 16,
                TextAnchor.MiddleLeft, new Color(0.0f, 0.9f, 0.4f));

            // Информация об изменениях в стиле терминала
            if (!string.IsNullOrEmpty(_changelog))
            {
                // Создаем фон для лога изменений в стиле терминала
                GameObject changelogBg = new GameObject("ChangelogBackground");
                changelogBg.transform.SetParent(panel.transform, false);
                Image changelogImage = changelogBg.AddComponent<Image>();
                changelogImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); // Почти черный фон
                RectTransform changelogRect = changelogBg.GetComponent<RectTransform>();
                changelogRect.sizeDelta = new Vector2(520, 170);
                changelogRect.anchoredPosition = new Vector2(0, -60);

                // Заголовок секции
                CreateHackerText(panel, "> Changelog:", new Vector2(-180, -5), 16,
                    TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));

                // Текст изменений в стиле консоли
                GameObject changelogText = new GameObject("ChangelogText");
                changelogText.transform.SetParent(changelogBg.transform, false);
                Text text = changelogText.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("Courier New.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 14;
                text.alignment = TextAnchor.UpperLeft;
                text.color = new Color(0.0f, 0.8f, 0.3f); // Терминально-зеленый текст
                text.text = _changelog;

                RectTransform textRectTransform = changelogText.GetComponent<RectTransform>();
                textRectTransform.sizeDelta = new Vector2(500, 150);
                textRectTransform.anchoredPosition = new Vector2(0, 0);

                // Добавляем скролл для длинных текстов
                ScrollRect scrollRect = changelogBg.AddComponent<ScrollRect>();
                scrollRect.content = textRectTransform;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;

                // Добавляем скроллбар в хакерском стиле
                GameObject scrollbar = new GameObject("Scrollbar");
                scrollbar.transform.SetParent(changelogBg.transform, false);
                Scrollbar scrl = scrollbar.AddComponent<Scrollbar>();
                Image scrollbarImage = scrollbar.AddComponent<Image>();
                scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                GameObject slidingArea = new GameObject("SlidingArea");
                slidingArea.transform.SetParent(scrollbar.transform, false);
                RectTransform slidingAreaRect = slidingArea.AddComponent<RectTransform>();
                slidingAreaRect.anchorMin = Vector2.zero;
                slidingAreaRect.anchorMax = Vector2.one;
                slidingAreaRect.sizeDelta = Vector2.zero;

                GameObject handle = new GameObject("Handle");
                handle.transform.SetParent(slidingArea.transform, false);
                Image handleImage = handle.AddComponent<Image>();
                handleImage.color = new Color(0.0f, 0.7f, 0.3f, 0.8f); // Зеленый хакерский стиль

                scrl.handleRect = handle.GetComponent<RectTransform>();
                scrl.direction = Scrollbar.Direction.BottomToTop;

                RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.pivot = new Vector2(1, 0.5f);
                scrollbarRect.sizeDelta = new Vector2(15, 0);
                scrollbarRect.anchoredPosition = new Vector2(7.5f, 0);

                scrollRect.verticalScrollbar = scrl;
            }

            // Кнопки в стиле терминала
            CreateHackerButton(panel, "INSTALL", new Vector2(-80, -160), () => {
                InstallUpdate();
            });

            CreateHackerButton(panel, "CANCEL", new Vector2(80, -160), () => {
                GameObject.Destroy(_updateWindow);
            });

            LogDebug("Окно обновления в стиле Grey Hack успешно создано");
        }

        // Создание текста в хакерском стиле
        private void CreateHackerText(GameObject parent, string message, Vector2 position, int fontSize,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("Courier New.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color ?? new Color(0.0f, 0.8f, 0.3f); // По умолчанию зеленый хакерский текст

            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(500, 30);
            rectTransform.anchoredPosition = position;
        }

        // Создание кнопки в хакерском стиле
        private void CreateHackerButton(GameObject parent, string label, Vector2 position, Action onClick)
        {
            GameObject buttonObj = new GameObject(label + "Button");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.08f, 0.08f, 0.08f);

            // Добавляем эффект обводки в стиле терминала
            Outline buttonOutline = buttonObj.AddComponent<Outline>();
            buttonOutline.effectColor = new Color(0.0f, 0.7f, 0.3f, 0.8f); // Зеленая обводка
            buttonOutline.effectDistance = new Vector2(1, 1);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Цвета кнопки при наведении/нажатии в хакерском стиле
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.08f, 0.08f);
            colors.highlightedColor = new Color(0.1f, 0.3f, 0.2f);
            colors.pressedColor = new Color(0.0f, 0.4f, 0.2f);
            colors.selectedColor = new Color(0.0f, 0.5f, 0.25f);
            button.colors = colors;

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 30);
            rectTransform.anchoredPosition = position;

            // Текст кнопки в стиле терминала
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Courier New.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.0f, 0.9f, 0.4f); // Зеленый хакерский текст

            RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
            textRectTransform.sizeDelta = new Vector2(120, 30);
            textRectTransform.anchoredPosition = Vector2.zero;

            // Добавляем обработчик нажатия
            button.onClick.AddListener(() => onClick());
        }

        private async void InstallUpdate()
        {
            LogDebug("======== НАЧАЛО УСТАНОВКИ ОБНОВЛЕНИЯ ========");
            LogDebug($"Установка обновления с {_currentVersion} до {_latestVersion}");
            LogDebug($"URL загрузки: {_downloadUrl}");

            try
            {
                // Показываем прогресс загрузки
                UpdateButtonText("Обновить", "Загрузка...");

                // Путь для скачивания и распаковки
                string tempPath = Path.Combine(Path.GetTempPath(), "GreyHackRus_update");
                string zipPath = Path.Combine(tempPath, "update.zip");
                string extractPath = Path.Combine(tempPath, "extracted");

                LogDebug($"Временный путь: {tempPath}");
                LogDebug($"Путь для Zip файла: {zipPath}");
                LogDebug($"Путь для распаковки: {extractPath}");

                // Создаем временные директории, если их нет
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(extractPath);

                // Скачиваем файл
                LogDebug("Начало загрузки файла обновления...");
                var downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "GreyHackRusPlugin");
                    client.DownloadProgressChanged += (sender, args) =>
                    {
                        if (args.ProgressPercentage % 10 == 0) // Логируем каждые 10%
                        {
                            LogDebug($"Прогресс загрузки: {args.ProgressPercentage}% ({args.BytesReceived}/{args.TotalBytesToReceive} байт)");
                        }

                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            UpdateButtonText("Обновить", $"Загрузка: {args.ProgressPercentage}%");
                        });
                    };

                    await client.DownloadFileTaskAsync(_downloadUrl, zipPath);
                }

                downloadStopwatch.Stop();
                LogDebug($"Файл успешно загружен за {downloadStopwatch.ElapsedMilliseconds} мс");
                LogDebug($"Размер файла: {new FileInfo(zipPath).Length} байт");

                UpdateButtonText("Обновить", "Распаковка...");
                LogDebug("Начало распаковки архива...");

                // Распаковываем архив и копируем файлы
                await Task.Run(() =>
                {
                    try
                    {
                        // Распаковываем Zip архив с использованием .NET
                        LogDebug("Открытие ZIP архива...");
                        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                        {
                            LogDebug($"Архив открыт, найдено {archive.Entries.Count} файлов");

                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                string destinationPath = Path.Combine(extractPath, entry.FullName);
                                LogDebug($"Распаковка: {entry.FullName} -> {destinationPath}");

                                // Пропускаем каталоги (которые не содержат файлов)
                                if (string.IsNullOrEmpty(entry.Name))
                                    continue;

                                // Создаем директорию, если её нет
                                string directoryName = Path.GetDirectoryName(destinationPath);
                                if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                                    Directory.CreateDirectory(directoryName);

                                // Извлекаем файл
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }

                        LogDebug("Архив успешно распакован");

                        // Копируем распакованные файлы в директорию плагина
                        LogDebug($"Копирование файлов в директорию плагина: {_pluginDirectoryPath}");
                        DirectoryCopy(extractPath, _pluginDirectoryPath, true);
                        LogDebug("Копирование файлов завершено");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Ошибка при распаковке и копировании файлов: {ex.Message}", DebugLogger.LogLevel.Error);
                        if (_debugLog != null)
                        {
                            _debugLog.LogStackTrace();
                        }
                        throw; // Пробрасываем исключение дальше для обработки
                    }
                });

                LogDebug("Обновление успешно установлено!");

                // Обновляем текст на кнопке
                UpdateButtonText("Обновить", "Готово!");

                // Показываем сообщение о необходимости перезапуска
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (_updateWindow != null)
                    {
                        Transform panel = _updateWindow.transform.Find("Panel");
                        if (panel != null)
                        {
                            // Создаем текст с информацией о необходимости перезапуска
                            GameObject restartText = new GameObject("RestartInfo");
                            restartText.transform.SetParent(panel.transform, false);
                            Text text = restartText.AddComponent<Text>();
                            text.text = "Для применения обновления\nтребуется перезапуск игры";
                            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                            text.fontSize = 16;
                            text.alignment = TextAnchor.MiddleCenter;
                            text.color = new Color(1f, 0.8f, 0.2f); // Желтоватый цвет для важности

                            RectTransform rectTransform = restartText.GetComponent<RectTransform>();
                            rectTransform.sizeDelta = new Vector2(400, 60);
                            rectTransform.anchoredPosition = new Vector2(0, -40);
                        }
                    }
                });

                // Изменяем назначение кнопки "Обновить" на "OK"
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (_updateWindow != null)
                    {
                        Transform button = _updateWindow.transform.Find("Panel/ОбновитьButton");
                        if (button != null)
                        {
                            Button btnComponent = button.GetComponent<Button>();
                            if (btnComponent != null)
                            {
                                // Очищаем старые обработчики
                                btnComponent.onClick.RemoveAllListeners();

                                // Добавляем новый обработчик, который просто закрывает окно
                                btnComponent.onClick.AddListener(() => {
                                    GameObject.Destroy(_updateWindow);
                                });

                                // Меняем текст
                                Transform textTransform = button.Find("Text");
                                if (textTransform != null)
                                {
                                    Text text = textTransform.GetComponent<Text>();
                                    if (text != null)
                                    {
                                        text.text = "OK";
                                    }
                                }
                            }
                        }
                    }
                });

                // Очищаем временные файлы
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch (Exception ex)
                {
                    LogDebug($"Не удалось удалить временные файлы: {ex.Message}", DebugLogger.LogLevel.Warning);
                }

                LogDebug("======== КОНЕЦ УСТАНОВКИ ОБНОВЛЕНИЯ ========");
            }
            catch (Exception ex)
            {
                LogDebug($"Критическая ошибка установки обновления: {ex.Message}", DebugLogger.LogLevel.Error);
                if (_debugLog != null)
                {
                    _debugLog.LogStackTrace();
                }
                LogDebug("======== ОШИБКА УСТАНОВКИ ОБНОВЛЕНИЯ ========");

                UpdateButtonText("Обновить", "Ошибка!");

                // Показываем детали ошибки
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_updateWindow != null)
                    {
                        Transform panel = _updateWindow.transform.Find("Panel");
                        if (panel != null)
                        {
                            CreateHackerText(panel.gameObject, $"Ошибка: {ex.Message}", new Vector2(0, -20), 14);
                        }
                    }
                });
            }
        }

        private void UpdateButtonText(string buttonName, string newText)
        {
            if (_updateWindow == null) return;

            Transform button = _updateWindow.transform.Find($"Panel/{buttonName}Button");
            if (button != null)
            {
                Transform textTransform = button.Find("Text");
                if (textTransform != null)
                {
                    Text text = textTransform.GetComponent<Text>();
                    if (text != null)
                    {
                        text.text = newText;
                    }
                }
            }
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Получаем содержимое исходной директории
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            // Если исходная директория не существует, выходим
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Исходная директория не найдена: " + sourceDirName);
            }

            // Если директория назначения не существует, создаем её
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Копируем все файлы
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                // Пропускаем PDB файлы при обновлении - они могут быть заблокированы
                if (file.Extension.ToLower() == ".pdb")
                {
                    LogDebug($"Пропускаем PDB файл при копировании: {file.Name}", DebugLogger.LogLevel.Warning);
                    continue;
                }

                try
                {
                    string temppath = Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, true);
                }
                catch (IOException ex)
                {
                    LogDebug($"Не удалось скопировать файл {file.Name}: {ex.Message}", DebugLogger.LogLevel.Warning);
                    // Продолжаем работу даже если один файл не удалось скопировать
                }
            }

            // Если указано копировать поддиректории, копируем рекурсивно
            if (copySubDirs)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}