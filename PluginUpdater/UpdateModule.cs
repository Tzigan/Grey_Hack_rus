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
        private const string UPDATE_INFO_URL = "https://raw.githubusercontent.com/Tzigan/Grey_Hack_rus/tree/1.1.3-beta/version.json";

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
            LogDebug("Создание окна обновления");

            // Создаем окно с Canvas, панелью и текстом
            _updateWindow = new GameObject("UpdateWindow");
            GameObject.DontDestroyOnLoad(_updateWindow);

            Canvas canvas = _updateWindow.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Поверх всего UI

            CanvasScaler scaler = _updateWindow.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _updateWindow.AddComponent<GraphicRaycaster>();

            // Создаем затемненный фон
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_updateWindow.transform, false);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.8f);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // Создаем панель сообщения
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_updateWindow.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(500, 300);
            panelRect.anchoredPosition = Vector2.zero;

            // Заголовок
            CreateText(panel, "Доступно обновление!", new Vector2(0, 100), 24);

            // Информация о версиях
            CreateText(panel, $"Текущая версия: {_currentVersion}", new Vector2(0, 50), 18);
            CreateText(panel, $"Новая версия: {_latestVersion}", new Vector2(0, 20), 18);

            // Информация об изменениях, если есть
            if (!string.IsNullOrEmpty(_changelog))
            {
                CreateText(panel, "Изменения:", new Vector2(0, -10), 16);
                CreateText(panel, _changelog, new Vector2(0, -40), 14);
            }

            // Кнопки
            CreateButton(panel, "Обновить", new Vector2(-100, -80), () => {
                InstallUpdate();
            });

            CreateButton(panel, "Отмена", new Vector2(100, -80), () => {
                GameObject.Destroy(_updateWindow);
            });

            LogDebug("Окно обновления успешно создано");
        }

        private void CreateText(GameObject parent, string message, Vector2 position, int fontSize)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 30);
            rectTransform.anchoredPosition = position;
        }

        private void CreateButton(GameObject parent, string label, Vector2 position, Action onClick)
        {
            GameObject buttonObj = new GameObject(label + "Button");
            buttonObj.transform.SetParent(parent.transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Цвета кнопки при наведении/нажатии
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
            button.colors = colors;

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160, 40);
            rectTransform.anchoredPosition = position;

            // Текст кнопки
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
            textRectTransform.sizeDelta = new Vector2(160, 40);
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
                            CreateText(panel.gameObject, $"Ошибка: {ex.Message}", new Vector2(0, -20), 14);
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