using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace PotionCraftAlchemyMachineRecipes
{
    [BepInPlugin("com.fahlgorithm.potioncraftalchemymachinerecipies", "PotionCraftAlchemyMachineRecipes", "0.5.0.1")]
    [BepInProcess("Potion Craft.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource PluginLogger {get; private set; }

        private void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "com.fahlgorithm.potioncraftalchemymachinerecipies");
            PluginLogger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID}: Patch Succeeded!");
        }
    }
}
