using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using GreyHackRussianPlugin.Translation;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GreyHackRussianPlugin
{
    // Основной класс плагина, наследуется от BaseUnityPlugin
    // Использует BepInEx для загрузки и управления плагинами
    // Патчинг с помощью HarmonyLib для изменения поведения игры

    [BepInPlugin("com.tzigan.greyhack.russian", "Grey Hack Russian", "1.0.0")]
    public class GreyHackRussianPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static string PluginPath;

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

            // Подписываемся на событие обновления Unity для анализа UI
            gameObject.AddComponent<UIAnalyzer>();
        }

        // Кэшированные значения для повышения производительности
        private static Type inputType;
        private static MethodInfo getKeyDownMethod;

        private bool IsKeyPressed(int keyCode)
        {
            try
            {
                // Инициализация при первом вызове
                if (inputType == null)
                    inputType = Type.GetType("UnityEngine.Input, UnityEngine");
                if (getKeyDownMethod == null && inputType != null)
                    getKeyDownMethod = inputType.GetMethod("GetKeyDown", BindingFlags.Public | BindingFlags.Static);

                if (getKeyDownMethod != null)
                    return (bool)getKeyDownMethod.Invoke(null, new object[] { (KeyCode)keyCode });
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка при проверке нажатия клавиши: {ex.Message}");
            }
            return false;
        }

        void Update()
        {
            // Используйте метод IsKeyPressed (115 - код клавиши F4)
            if (IsKeyPressed(115))
            {
                Patches.ExploitPatch.RequestExport();
                Log.LogInfo("Запрошен экспорт переводов по нажатию F4");
            }

            Patches.ExploitPatch.OnUpdate();
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