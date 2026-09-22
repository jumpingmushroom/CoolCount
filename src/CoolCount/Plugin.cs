using BepInEx;
using BepInEx.Logging;
using CoolCount.Core;
using HarmonyLib;

namespace CoolCount
{
    /// <summary>
    /// OmniCC for Valheim. Two Harmony postfixes, one on the inventory grid and one on the
    /// hotbar, add a countdown, a radial sweep and a dimmed icon to every consumable the
    /// player cannot use yet. Purely client-side; nothing is sent or stored.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    public sealed class CoolCountPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jumpingmushroom.coolcount";
        public const string PluginName = "CoolCount";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Bind(base.Config);
            ConsoleCommands.Register();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(CoolCountPlugin).Assembly);

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
    }
}
