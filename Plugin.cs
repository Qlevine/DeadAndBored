using BepInEx;
using HarmonyLib;

namespace DeadAndBored
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalCompanyInputUtils.LethalCompanyInputUtilsPlugin.ModId, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SpectateEnemy.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        public static InputActions inputActions = new InputActions();
        private void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            Configuration.Config.Init();
            Logger.LogInfo("Dead And Bored loaded!");
            harmony.PatchAll();
        }
    }
}