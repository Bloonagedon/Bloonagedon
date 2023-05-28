using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using System.IO;

namespace inscryption_multiplayer
{

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        public const string PluginGuid = "inscryption.multiplayer";
        public const string PluginName = "Inscryption Multiplayer";
        public const string PluginVersion = "1.0.0";

        public static string Directory;

        public static bool MultiplayerActive => InscryptionNetworking.Connection.Connected;

        public void Awake()
        {
            // Plugin startup logic
            base.Logger.LogInfo("Loaded inscryption multiplayer!");
            Plugin.Log = base.Logger;

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Directory = Path.GetDirectoryName(base.Info.Location);
            MultiplayerAssetHandler.LoadBundle();
        }

        public void Update()
        {
            InscryptionNetworking.Connection.Update();
        }

        public void OnApplicationQuit()
        {
            InscryptionNetworking.Connection.Dispose();
        }
    }
}
