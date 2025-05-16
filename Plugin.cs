using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using GreyHackRussianPlugin.DebugTools;
using GreyHackRussianPlugin.Translation;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using GreyHackRussianPlugin.PluginUpdater;

namespace GreyHackRussianPlugin
{
    [BepInPlugin("com.tzigan.greyhack.russian", "Grey Hack Russian", "1.1.4")]
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

                // Инициализируем ExploitPatch 
                Patches.ExploitPatch.Initialize();

                // Диагностика ExploitPatch после обновления игры
                try
                {
                    var exploitType = AccessTools.TypeByName("Exploit");
                    if (exploitType != null)
                    {
                        var method = AccessTools.Method(exploitType, "GetShopDescription");
                        if (method != null)
                        {
                            Log.LogInfo("Патч ExploitPatch применен успешно");
                        }
                        else
                        {
                            Log.LogError("Метод GetShopDescription не найден в классе Exploit!");

                            // Поиск похожих методов для подсказки
                            foreach (var m in exploitType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            {
                                if (m.Name.Contains("Description") || m.Name.Contains("Shop"))
                                {
                                    Log.LogInfo($"Найден похожий метод: {m.Name} в {exploitType.Name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.LogError("Класс Exploit не найден. Патч ExploitPatch не будет применен.");

                        // Поиск похожих классов во всех загруженных сборках
                        Log.LogInfo("Поиск похожих классов в загруженных сборках...");
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                foreach (var type in assembly.GetTypes())
                                {
                                    if (type.Name.Contains("Exploit") || type.Name.Contains("exploit"))
                                    {
                                        Log.LogInfo($"Найден похожий тип: {type.FullName} в сборке {assembly.GetName().Name}");
                                    }
                                }
                            }
                            catch (ReflectionTypeLoadException)
                            {
                                // Игнорируем сборки, которые не могут быть загружены
                                continue;
                            }
                            catch (Exception ex)
                            {
                                Log.LogWarning($"Ошибка при сканировании сборки {assembly.GetName().Name}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogError($"Ошибка при проверке патча ExploitPatch: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка при применении патчей: {ex.Message}\n{ex.StackTrace}");
            }

            // Только для логирования, без API запросов
            try
            {
                DebugLog = new DebugLogger(Log, PluginPath);
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
                    "1.1.4",
                    PluginPath,
                    Log,
                    DebugLog
                );

                // Запускаем проверку обновлений только если нет режима отладки
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