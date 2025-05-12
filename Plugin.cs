using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using GreyHackRussianPlugin.Translation;
using GreyHackRussianPlugin.Patches;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
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

            try
            {
                var itemShopType = AccessTools.TypeByName("ItemShop");
                UnityEngine.Debug.Log($"[GreyHackRussian] ItemShop тип найден: {itemShopType != null}");

                var preBuyType = AccessTools.TypeByName("PreBuy");
                UnityEngine.Debug.Log($"[GreyHackRussian] PreBuy тип найден: {preBuyType != null}");

                // Проверка пространства имен
                if (itemShopType != null)
                {
                    UnityEngine.Debug.Log($"[GreyHackRussian] ItemShop namespace: {itemShopType.Namespace}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GreyHackRussian] Ошибка проверки типов: {ex.Message}");
            }

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

                try
                {
                    Log.LogInfo("Явная регистрация ShopPatch...");
                    harmony.PatchAll(typeof(ShopPatch));
                    Log.LogInfo("ShopPatch успешно зарегистрирован");

                    // Инициализируем ShopPatch после успешной регистрации
                    ShopPatch.Initialize();
                    Log.LogInfo("ShopPatch успешно инициализирован");

                    // Регистрация и инициализация ShopDetailsPatch
                    Log.LogInfo("Явная регистрация ShopDetailsPatch...");
                    harmony.PatchAll(typeof(ShopDetailsPatch));
                    ShopDetailsPatch.Initialize();
                    Log.LogInfo("ShopDetailsPatch успешно инициализирован");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Ошибка регистрации ShopPatch: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка при применении патчей: {ex.Message}\n{ex.StackTrace}");
            }

            // Подписываемся на событие обновления Unity для анализа UI
            gameObject.AddComponent<UIAnalyzer>();

            // Вывод информации о методах для отладки
            foreach (var method in typeof(ItemShop).GetMethods())
            {
                if (method.Name == "Configure")
                {
                    string paramInfo = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    UnityEngine.Debug.Log($"[GreyHackRussian] ItemShop.Configure метод: ({paramInfo})");
                }
            }

            foreach (var method in typeof(PreBuy).GetMethods())
            {
                if (method.Name == "Configure")
                {
                    string paramInfo = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    UnityEngine.Debug.Log($"[GreyHackRussian] PreBuy.Configure метод: ({paramInfo})");
                }
            }
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