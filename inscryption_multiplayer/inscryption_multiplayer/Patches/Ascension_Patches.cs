using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Ascension_Patches
    {
        public static bool DeckSelection;

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

        [HarmonyPatch(typeof(AscensionMenuScreens), nameof(AscensionMenuScreens.SwitchToScreen))]
        [HarmonyPrefix]
        private static bool ExitAscensionAfterMatch(AscensionMenuScreens __instance, AscensionMenuScreens.Screen screen)
        {
            if (Menu_Patches.MultiplayerError != null || Plugin.MultiplayerActive)
            {
                if (DeckSelection && screen == AscensionMenuScreens.Screen.SelectChallenges)
                {
                    DeckSelection = false;
                    SaveManager.savingDisabled = true;
                    AscensionSaveData.Data.NewRun(StarterDecksUtil.GetInfo(AscensionSaveData.Data.currentStarterDeck).cards);
                    LoadingScreenManager.LoadScene("Part1_Cabin");
                    Singleton<InteractionCursor>.Instance.SetHidden(true);
                    Plugin.Log.LogInfo("started a game!");
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
