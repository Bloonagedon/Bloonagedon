using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using Steamworks;
using System.Text;
using UnityEngine;

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

        protected Callback<LobbyEnter_t> Callback_lobbyEnter;
        protected Callback<LobbyChatMsg_t> Callback_lobbyChatMsgReceived;

        public void Awake()
        {
            // Plugin startup logic
            base.Logger.LogInfo("Loaded inscryption multiplayer!");
            Plugin.Log = base.Logger;

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            Directory = base.Info.Location;

            Networking.SteamNetworking.SpaceByte = Encoding.UTF8.GetBytes(" ");
        }

        public void Update()
        {
            SteamAPI.RunCallbacks();
            if (Input.GetKeyDown(KeyCode.M))
            {
                MakeAllNodesMultiplayerNodes();
            }
        }

        //this is some very bad code to temporary test stuff as i couldn't get it to work in any other way
        public void MakeAllNodesMultiplayerNodes()
        {
            if (Singleton<MapNodeManager>.Instance?.nodes != null)
            {
                for (int i = 0; i < Singleton<MapNodeManager>.Instance.nodes.Count; i++)
                {
                    Plugin.Log.LogInfo(Singleton<MapNodeManager>.Instance.nodes[i].Data.GetType());
                    if (Singleton<MapNodeManager>.Instance.nodes[i].Data.GetType() == typeof(CardBattleNodeData))
                    {
                        ((CardBattleNodeData)Singleton<MapNodeManager>.Instance.nodes[i].Data).specialBattleId = "Multiplayer_Battle_Sequencer";
                    }
                }
            }
        }
    }
}
