using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GreyHackRussianPlugin.DebugTools
{
    /// <summary>
    /// Расширенное логирование с сохранением в файл и дополнительной информацией
    /// </summary>
    public class DebugLogger
    {
        private readonly ManualLogSource _logger;
        private readonly string _logFilePath;
        private static bool _isEnabled = true;
        private List<string> _memoryLog = new List<string>();
        private readonly int _maxMemoryLogSize = 1000;

        public DebugLogger(ManualLogSource logger, string pluginPath)
        {
            _logger = logger;
            _logFilePath = Path.Combine(pluginPath, "debug_log.txt");

            // Создаем или очищаем файл лога при запуске
            File.WriteAllText(_logFilePath, $"=== Debug Log Start: {DateTime.Now} ===\n");

            LogToFile($"Plugin Path: {pluginPath}");
            LogToFile($"Unity Version: {Application.unityVersion}");
            LogToFile($"OS: {SystemInfo.operatingSystem}");
            LogToFile($"Device: {SystemInfo.deviceModel}");
            LogToFile($"Processor: {SystemInfo.processorType}");
            LogToFile($"Memory: {SystemInfo.systemMemorySize} MB");
            LogToFile(new string('-', 50));
        }

        /// <summary>
        /// Включение или выключение отладки
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Выводит сообщение в лог с сохранением в файл
        /// </summary>
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!_isEnabled) return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] {message}";

            // Логируем через BepInEx
            switch (level)
            {
                case LogLevel.Info:
                    _logger.LogInfo(formattedMessage);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    _logger.LogError(formattedMessage);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug(formattedMessage);
                    break;
            }

            // Сохраняем в файл
            LogToFile(formattedMessage);

            // Добавляем в память
            _memoryLog.Add(formattedMessage);
            if (_memoryLog.Count > _maxMemoryLogSize)
            {
                _memoryLog.RemoveAt(0);
            }
        }

        /// <summary>
        /// Записывает в файл лога
        /// </summary>
        private void LogToFile(string message)
        {
            try
            {
                File.AppendAllText(_logFilePath, message + "\n");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Не удалось записать в файл лога: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает последние N сообщений из лога
        /// </summary>
        public string[] GetRecentLogs(int count = 50)
        {
            count = Math.Min(count, _memoryLog.Count);
            string[] result = new string[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = _memoryLog[_memoryLog.Count - count + i];
            }

            return result;
        }

        /// <summary>
        /// Выводит текущее состояние стека вызовов
        /// </summary>
        public void LogStackTrace()
        {
            if (!_isEnabled) return;

            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            StringBuilder sb = new StringBuilder("Stack Trace:\n");

            for (int i = 1; i < stackTrace.FrameCount; i++) // начинаем с 1, чтобы пропустить текущий метод
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame.GetMethod();
                sb.AppendLine($"  at {method.DeclaringType?.FullName ?? "unknown"}.{method.Name}() in {frame.GetFileName() ?? "unknown"}:{frame.GetFileLineNumber()}");
            }

            Log(sb.ToString());
        }

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Debug
        }
    }
}