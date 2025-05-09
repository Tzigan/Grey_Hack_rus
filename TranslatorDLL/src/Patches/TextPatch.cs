using HarmonyLib;
using GreyHackTranslator;

namespace GreyHackTranslator.Patches
{
    // Патч для стандартных UI.Text компонентов Unity
    [HarmonyPatch(typeof(UnityEngine.UI.Text), "set_text")]
    public class TextPatch
    {
        static void Prefix(ref string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string original = value;
                value = TranslatorPlugin.TranslateText(value);

                // Логируем перевод только если он изменился
                if (value != original)
                {
                    TranslatorPlugin.Log($"Перевод UI: '{original}' -> '{value}'");
                }
            }
        }
    }
}