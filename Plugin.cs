using BepInEx;
using HarmonyLib;

namespace DeadAndBored
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SpectateEnemy.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(LC_API.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            Configuration.Config.Init();
            Logger.LogInfo("Dead And Bored loaded!");
            harmony.PatchAll();
        }
    }
}