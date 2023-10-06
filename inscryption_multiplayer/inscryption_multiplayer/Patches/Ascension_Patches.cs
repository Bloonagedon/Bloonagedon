using BepInEx.Bootstrap;
using DiskCardGame;
using HarmonyLib;
using Infiniscryption.PackManagement;
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

        public static bool PackManagerActive => Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.packmanager");
        public static bool SideDeckSelectorActive => Chainloader.PluginInfos.ContainsKey("zorro.inscryption.infiniscryption.sidedecks");

        public class ChosenChallenges
        {
            public List<AscensionChallenge> challenges { get; set; }
        }
        public class ChosenPacks
        {
            public string activePackString { get; set; }
            public string activePackKey { get; set; }
            public string inactivePackString { get; set; }
            public string inactivePackKey { get; set; }
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
            if (PackManagerActive || SideDeckSelectorActive)
            {
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

                //Plugin.Log.LogInfo($"Current screen: {__instance.CurrentScreen}\nPrevious screen: {__instance.PreviousScreen}");

                //From deck selection to challenges
                if (DeckSelection && __instance.CurrentScreen == AscensionMenuScreens.Screen.SelectChallenges)
                {
                    DeckSelection = false;

                    if (InscryptionNetworking.Connection.IsHost)
                    {
                        __instance.StartCoroutine(__instance.ScreenSwitchSequence(AscensionMenuScreens.Screen.SelectChallenges));
                        return false;
                    }
                    else
                    {
                        if (SideDeckSelectorActive)
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

                //From challenges to another screen
                if (__instance.PreviousScreen == AscensionMenuScreens.Screen.SelectChallenges)
                {
                    if (PackManagerActive)
                    {
                        __instance.StartCoroutine(__instance.ScreenSwitchSequence(PackSelectorScreen.Key));
                    }
                    else if (SideDeckSelectorActive)
                    {
                        __instance.StartCoroutine(__instance.ScreenSwitchSequence(SideDeckSelectorScreen.Key));
                    }
                    else
                    {
                        HostStartRun();
                    }
                    return false;
                }

                //from pack selector to another screen
                if (PackManagerActive && SideDeckSelectorActive)
                {
                    if (__instance.PreviousScreen == PackSelectorScreen.Key)
                    {
                        __instance.StartCoroutine(__instance.ScreenSwitchSequence(SideDeckSelectorScreen.Key));
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
                //from any screen to the game
                if (InscryptionNetworking.Connection.IsHost)
                {
                    HostStartRun();
                }
                else
                {
                    __instance.StartCoroutine(WaitUntilGameStarts());
                }
                return false;
            }
            return true;
        }


        public static void HostStartRun()
        {
            HostSelection = false;

            if (PackManagerActive)
            {
                SendActivePacks();
            }

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

        public static void SendActivePacks()
        {
            MethodInfo SavePackListMethod = typeof(PackManager).GetMethod("RetrievePackList", BindingFlags.NonPublic | BindingFlags.Static);

            List<PackInfo> activePacks = (List<PackInfo>)SavePackListMethod.Invoke(null, new object[] { true, CardTemple.Nature });
            List<PackInfo> inactivePacks = (List<PackInfo>)SavePackListMethod.Invoke(null, new object[] { false, CardTemple.Nature });

            ChosenPacks chosenPacks = new ChosenPacks
            {
                activePackString = String.Join("|", activePacks.Select(pi => pi.Key)),
                activePackKey = $"AscensionData_ActivePackList",
                inactivePackString = String.Join("|", inactivePacks.Select(pi => pi.Key)),
                inactivePackKey = $"{CardTemple.Nature}_InactivePackList"
            };
            InscryptionNetworking.Connection.SendJson(NetworkingMessage.PacksChosen, chosenPacks);
        }

        public static IEnumerator WaitUntilGameStarts()
        {
            GameObject starterDeckSelectScreen = Singleton<AscensionMenuScreens>.Instance.starterDeckSelectScreen;

            GameObject HeaderText = starterDeckSelectScreen.transform.Find("Header/Mid").gameObject;
            GameObject WaitForChallengesText = GameObject.Instantiate(HeaderText);

            Singleton<AscensionMenuScreens>.Instance.DeactivateAllScreens();

            WaitForChallengesText.transform.SetParent(starterDeckSelectScreen.transform.parent);
            WaitForChallengesText.transform.position = new Vector3(0, 0, 0);
            WaitForChallengesText.GetComponentInChildren<Text>().text = "Waiting for the host to pick the challenges" + (PackManagerActive ? " and packs" : "");
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
                HostSelection = true;

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
