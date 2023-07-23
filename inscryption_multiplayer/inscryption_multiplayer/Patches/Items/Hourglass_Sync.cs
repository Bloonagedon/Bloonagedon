using System.Collections;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;
using UnityEngine;

[HarmonyPatch]
public class Hourglass_Sync
{
    [HarmonyPatch(typeof(HourglassItem), nameof(HourglassItem.ActivateSequence))]
    [HarmonyPrefix]
    public static bool SkipTurn(HourglassItem __instance, ref IEnumerator __result)
    {
        if(!Plugin.MultiplayerActive || !MultiplayerRunState.Run.OpponentItemUsed)
            return true;
        __result = SkipTurnSequence(__instance);
        return false;
    }
    
    private static IEnumerator SkipTurnSequence(HourglassItem item)
    {
        item.PlayExitAnimation();
        yield return new WaitForSeconds(.25f);
        MultiplayerRunState.Run.SkipNextTurn = true;
        Singleton<TextDisplayer>.Instance.StartCoroutine(Singleton<TextDisplayer>.Instance.ShowUntilInput("You'll pass your next turn.", -0.65f, 0.4f));
    }
}