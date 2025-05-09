using HarmonyLib;
using System.Reflection;
using GreyHackTranslator;

namespace GreyHackTranslator.Patches
{
    // Здесь нужно будет адаптировать патчи под конкретные классы в игре Grey Hack
    
    [HarmonyPatch]
    public class DialogPatch
    {
        static MethodBase TargetMethod()
        {
            // Пример для класса и метода отображения диалога
            var type = AccessTools.TypeByName("DialogManager");
            if (type != null)
            {
                return AccessTools.Method(type, "ShowDialog");
            }
            
            // Можно добавить поиск альтернативных методов
            var uiManager = AccessTools.TypeByName("UIManager");
            if (uiManager != null)
            {
                return AccessTools.Method(uiManager, "ShowMessage");
            }
            
            return null;
        }
        
        static void Prefix(ref string __0)
        {
            if (__0 != null)
            {
                string original = __0;
                __0 = TranslatorPlugin.TranslateText(__0);
                
                if (__0 != original)
                {
                    TranslatorPlugin.Log($"Перевод диалога: '{original}' -> '{__0}'");
                }
            }
        }
    }
    
    // Патч для компьютерных терминалов (специфично для Grey Hack)
    [HarmonyPatch]
    public class TerminalPatch
    {
        static MethodBase TargetMethod()
        {
            // Найти класс терминала и метод вывода текста
            var terminal = AccessTools.TypeByName("Terminal");
            if (terminal != null)
            {
                return AccessTools.Method(terminal, "PrintText");
            }
            
            return null;
        }
        
        static void Prefix(ref string __0)
        {
            if (__0 != null)
            {
                string original = __0;
                __0 = TranslatorPlugin.TranslateText(__0);
                
                if (__0 != original)
                {
                    TranslatorPlugin.Log($"Перевод терминала: '{original}' -> '{__0}'");
                }
            }
        }
    }
}