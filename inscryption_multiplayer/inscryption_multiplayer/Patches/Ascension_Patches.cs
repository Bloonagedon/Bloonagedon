using BepInEx.Bootstrap;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using InscryptionAPI.Ascension;
using InscryptionAPI.Saves;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Ascension_Patches
    {
        public static bool DeckSelection;
        public static bool HostSelection;

        public static KeyValuePair<AscensionMenuScreens.Screen, AscensionRunSetupScreenBase> PackSelectorScreen;
        public static KeyValuePair<AscensionMenuScreens.Screen, AscensionRunSetupScreenBase> SideDeckSelectorScreen;

        public class ChosenChallenges
        {
            public List<AscensionChallenge> challenges { get; set; }
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadFromFile))]
        [HarmonyPostfix]
        private static void SetAscension()
        {
            if (Plugin.MultiplayerActive)
                SaveFile.IsAscension = true;
        }

        [HarmonyPatch(typeof(MenuController), nameof(MenuController.TransitionToAscensionMenu))]
        [HarmonyPrefix]
        private static void ReplaceAscension(MenuController __instance, ref IEnumerator __result)
        {
            if (!DeckSelection && Plugin.MultiplayerActive)
            {
                InscryptionNetworking.Connection.Leave();
                SaveFile.IsAscension = false;
                Menu_Patches.MultiplayerError = NetworkingError.GaveUp;
            }
        }

        [HarmonyPatch(typeof(AscensionScreenManager), "InitializeScreensOnStart")]
        [HarmonyPostfix]
        public static void InitializeScreensOnStart()
        {
            if (Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.packmanager") || Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.sidedecks"))
            {
                AscensionScreenManager.InitializeAllScreens();

                Type type = typeof(AscensionScreenManager);
                FieldInfo info = type.GetField("screens", BindingFlags.NonPublic | BindingFlags.Static);
                Dictionary<AscensionMenuScreens.Screen, AscensionRunSetupScreenBase> screens = (Dictionary<AscensionMenuScreens.Screen, AscensionRunSetupScreenBase>)info.GetValue(null);

                foreach (KeyValuePair<AscensionMenuScreens.Screen, AscensionRunSetupScreenBase> ScreenInfo in screens)
                {
                    if (ScreenInfo.Value.name == "PackSelectorScreen")
                    {
                        PackSelectorScreen = ScreenInfo;
                    }
                    if (ScreenInfo.Value.name == "SideDeckSelectorScreen")
                    {
                        SideDeckSelectorScreen = ScreenInfo;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AscensionMenuScreens), nameof(AscensionMenuScreens.SwitchToScreen))]
        [HarmonyPrefix]
        private static bool ExitAscensionAfterMatch(AscensionMenuScreens __instance, AscensionMenuScreens.Screen screen)
        {
            if (Menu_Patches.MultiplayerError != null || Plugin.MultiplayerActive)
            {
                __instance.PreviousScreen = __instance.CurrentScreen;
                __instance.CurrentScreen = screen;

                //From deck selection to challenges or sidedeck selector
                if (DeckSelection && __instance.CurrentScreen == AscensionMenuScreens.Screen.SelectChallenges)
                {
                    DeckSelection = false;
                    HostSelection = true;

                    if (!InscryptionNetworking.Connection.IsHost)
                    {
                        __instance.StartCoroutine(__instance.ScreenSwitchSequence(AscensionMenuScreens.Screen.SelectChallenges));
                        return false;
                    }
                    else
                    {
                        if (Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.sidedecks"))
                        {
                            __instance.StartCoroutine(__instance.ScreenSwitchSequence(SideDeckSelectorScreen.Key));
                        }
                        else
                        {
                            __instance.StartCoroutine(WaitUntilGameStarts());
                        }

                        return false;
                    }
                }

                //From challenges to game or sidedeck selector
                if (HostSelection && __instance.PreviousScreen == AscensionMenuScreens.Screen.SelectChallenges)
                {
                    if (Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.sidedecks"))
                    {
                        __instance.StartCoroutine(__instance.ScreenSwitchSequence(SideDeckSelectorScreen.Key));
                    }
                    else
                    {
                        HostStartRun();
                    }
                    return false;
                }

                //Plugin.MultiplayerActive = false;
                Menu_Patches.MultiplayerError = null;
                InscryptionNetworking.Connection.Leave();
                __instance.ExitAscension();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(AscensionMenuScreens), nameof(AscensionMenuScreens.TransitionToGame))]
        [HarmonyPrefix]
        public static bool TransitionToGame(AscensionMenuScreens __instance)
        {
            if (Plugin.MultiplayerActive)
            {
                //From SideDeckSelector to game
                if (Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.sidedecks"))
                {
                    if (!InscryptionNetworking.Connection.IsHost)
                    {
                        HostStartRun();
                    }
                    else
                    {
                        __instance.StartCoroutine(WaitUntilGameStarts());
                    }
                    return false;
                }
            }
            return true;
        }


        public static void HostStartRun()
        {
            HostSelection = false;

            ChosenChallenges chosenChallenges = new ChosenChallenges
            {
                challenges = AscensionSaveData.Data.activeChallenges
            };
            InscryptionNetworking.Connection.SendJson(NetworkingMessage.ChallengesChosen, chosenChallenges);

            SaveManager.savingDisabled = true;
            AscensionSaveData.Data.NewRun(StarterDecksUtil.GetInfo(AscensionSaveData.Data.currentStarterDeck).cards);
            LoadingScreenManager.LoadScene("Part1_Cabin");
            Singleton<InteractionCursor>.Instance.SetHidden(true);
            Plugin.Log.LogInfo("started a game!");
        }

        public static IEnumerator WaitUntilGameStarts()
        {
            GameObject EndScreen = Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.sidedecks") ?
                                   SideDeckSelectorScreen.Value.gameObject : Singleton<AscensionChooseStarterDeckScreen>.Instance.gameObject;

            GameObject HeaderText = EndScreen.transform.Find("Header/Mid").gameObject;
            GameObject WaitForChallengesText = GameObject.Instantiate(HeaderText);

            foreach (Transform child in EndScreen.transform)
            {
                child.gameObject.SetActive(false);
            }

            WaitForChallengesText.transform.SetParent(EndScreen.transform);
            WaitForChallengesText.transform.position = new Vector3(0, 0, 0);
            WaitForChallengesText.GetComponentInChildren<Text>().text = "Waiting for the host to pick the challenges";
            WaitForChallengesText.SetActive(true);

            yield return new WaitUntil(() => !HostSelection);
            GameObject.Destroy(WaitForChallengesText);

            SaveManager.savingDisabled = true;
            AscensionSaveData.Data.NewRun(StarterDecksUtil.GetInfo(AscensionSaveData.Data.currentStarterDeck).cards);
            LoadingScreenManager.LoadScene("Part1_Cabin");
            Singleton<InteractionCursor>.Instance.SetHidden(true);
            Plugin.Log.LogInfo("started a game!");
        }

        [HarmonyPatch(typeof(AscensionMenuScreens), nameof(AscensionMenuScreens.ConfigurePostGameScreens))]
        [HarmonyPrefix]
        private static bool ForceDeckSelection(AscensionMenuScreens __instance)
        {
            if (DeckSelection && Plugin.MultiplayerActive)
            {
                __instance.StartCoroutine(
                    __instance.ScreenSwitchSequence(AscensionMenuScreens.Screen.StarterDeckSelect));
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(AscensionSaveData), nameof(AscensionSaveData.NewRun))]
        [HarmonyPrefix]
        private static void ReplaceNumRunsSinceReachedFirstBossStat(ref int ___numRunsSinceReachedFirstBoss, ref int __state)
        {
            if (Plugin.MultiplayerActive)
            {
                __state = ___numRunsSinceReachedFirstBoss;
                ___numRunsSinceReachedFirstBoss = 0;
            }
        }

        [HarmonyPatch(typeof(AscensionSaveData), nameof(AscensionSaveData.NewRun))]
        [HarmonyPostfix]
        private static void RestoreNumRunsSinceReachedFirstBossStat(ref int ___numRunsSinceReachedFirstBoss,
            ref int __state)
        {
            if (Plugin.MultiplayerActive)
                ___numRunsSinceReachedFirstBoss = __state;
        }

        [HarmonyPatch(typeof(AscensionSaveData), nameof(AscensionSaveData.EndRun))]
        [HarmonyPrefix]
        private static void DisableRunStats(ref RunState ___currentRun)
        {
            if (Plugin.MultiplayerActive)
                ___currentRun = null;
        }

        private static int GetNumRunsSinceReachedFirstBoss(int value)
        {
            return Plugin.MultiplayerActive ? 0 : value;
        }

        [HarmonyPatch(typeof(RunIntroSequencer), nameof(RunIntroSequencer.RunIntroSequence), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ReplaceNumRunsSinceReachedFirstBossInIntro(
            IEnumerable<CodeInstruction> instructions)
        {
            var getStatMethod = AccessTools.Method(typeof(Ascension_Patches),
                nameof(GetNumRunsSinceReachedFirstBoss));
            var numRunsSinceReachedFirstBossField = AccessTools.Field(typeof(AscensionSaveData),
                nameof(AscensionSaveData.numRunsSinceReachedFirstBoss));
            foreach (var inst in instructions)
            {
                yield return inst;
                if (inst.opcode == OpCodes.Ldfld && (FieldInfo)inst.operand == numRunsSinceReachedFirstBossField)
                    yield return new CodeInstruction(OpCodes.Call, getStatMethod);
            }
        }
    }
}
