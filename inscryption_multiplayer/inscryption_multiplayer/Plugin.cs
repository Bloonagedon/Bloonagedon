using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using InscryptionAPI.Card;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

            CardAppearanceBehaviour.Appearance LifeCounterApp =
                CardAppearanceBehaviourManager.Add(
                    PluginGuid,
                    "LifeCounterApp",
                    typeof(LifeCounter_Patches.LifeCounterBackground)).Id;

            LifeCounter_Patches.LifeCounterCardInfo = CardManager.New("multiplayer_scale_counter", "Life_Counter", "0", 0, 0);
            LifeCounter_Patches.LifeCounterCardInfo.AddAppearances(LifeCounterApp);
            LifeCounter_Patches.LifeCounterCardInfo.Mods = new List<CardModificationInfo>() { LifeCounter_Patches.nameReplacementMod };
            LifeCounter_Patches.LifeCounterCardInfo.hideAttackAndHealth = true;
        }

        public void Update()
        {
            InscryptionNetworking.Connection.Update();

            if (LifeManager.m_Instance != null && LifeCounter_Patches.LifeCounterObject != null)
            {
                if (LifeManager.m_Instance.Balance != LifeCounter_Patches.OldBalance)
                {
                    LifeCounter_Patches.OldBalance = LifeManager.Instance.Balance;
                    LifeCounter_Patches.UpdateLifeCounter();
                }
            }
        }

        public void OnApplicationQuit()
        {
            InscryptionNetworking.Connection.Dispose();
        }

        public void OnGUI()
        {
            DebugUI.OnGUI();
        }
    }
}
