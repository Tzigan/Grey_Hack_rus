using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using GreyHackRussianPlugin.DebugTools;
using GreyHackRussianPlugin.Translation;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using GreyHackRussianPlugin.PluginUpdater;

namespace GreyHackRussianPlugin
{
    // Основной класс плагина, наследуется от BaseUnityPlugin
    // Использует BepInEx для загрузки и управления плагинами
    // Патчинг с помощью HarmonyLib для изменения поведения игры

    [BepInPlugin("com.tzigan.greyhack.russian", "Grey Hack Russian", "1.1.0")]
    public class GreyHackRussianPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static string PluginPath;
        internal static DebugLogger DebugLog;
        private UpdateModule _updateModule;

        private void Awake()
        {
            // Инициализация логгера BepInEx
            Log = Logger;
            Log.LogInfo("Grey Hack Russian плагин запущен");

            // Получение пути к папке плагина
            PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Log.LogInfo($"Путь к плагину: {PluginPath}");

            // Загрузка переводов
            try
            {
                Translator.LoadTranslations();
                Log.LogInfo("Переводы успешно загружены");
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка загрузки переводов: {ex.Message}");
            }

            // Применение патчей Harmony
            try
            {
                var harmony = new Harmony("com.tzigan.greyhack.russian");
                harmony.PatchAll();
                Log.LogInfo("Патчи успешно применены");
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка при применении патчей: {ex.Message}\n{ex.StackTrace}");
            }

            // Только для логирования, без API запросов
            try
            {
                DebugLog = new DebugLogger(Log, PluginPath);
                // НЕ инициализируем ApiDebug
                DebugLog.Log("Базовое логирование инициализировано");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Ошибка инициализации логирования: {ex.Message}");
            }

            // Инициализация модуля обновлений
            try
            {
                _updateModule = new UpdateModule(
                    "1.1.0",                 // Текущая версия плагина
                    PluginPath,              // Путь к директории плагина
                    Log,                      // Логгер
                    DebugLog                 // Передаем экземпляр DebugLogger
                );

                // Запускаем проверку обновлений только если нет режима отладки (чтобы избежать двойных запросов)
#if !DEBUG
                _ = _updateModule.CheckForUpdates();
#endif
                Log.LogInfo("Модуль обновлений инициализирован");
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка при инициализации модуля обновлений: {ex.Message}");
            }

            // Подписываемся на событие обновления Unity для анализа UI
            gameObject.AddComponent<UIAnalyzer>();
        }
    }

    // Компонент для анализа UI (добавляется к объекту плагина)
    public class UIAnalyzer : MonoBehaviour
    {
        private bool hasAnalyzed = false;

        void Update()
        {
            // Анализируем UI только один раз через некоторое время после загрузки
            if (!hasAnalyzed && Time.timeSinceLevelLoad > 5f)
            {
                try
                {
                    GreyHackRussianPlugin.Log.LogInfo("Анализ UI компонентов...");
                    var allTexts = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
                    GreyHackRussianPlugin.Log.LogInfo($"Найдено {allTexts.Length} текстовых компонентов");

                    int count = 0;
                    foreach (var text in allTexts)
                    {
                        if (count++ < 10)
                        {
                            GreyHackRussianPlugin.Log.LogInfo($"Text[{count}]: '{text.text}' на объекте '{text.gameObject.name}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    GreyHackRussianPlugin.Log.LogError($"Ошибка при анализе UI: {ex.Message}");
                }

                hasAnalyzed = true;
            }
        }
    }
}