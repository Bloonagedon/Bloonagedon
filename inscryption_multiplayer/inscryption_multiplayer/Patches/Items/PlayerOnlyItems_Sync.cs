using System.Collections;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;
using UnityEngine;

//Disable events of items that only affect the player: bottles, hoggy bank, lens
[HarmonyPatch]
public class PlayerOnlyItems_Sync
{
    [HarmonyPatch(typeof(CardBottleItem), nameof(CardBottleItem.ActivateSequence))]
    [HarmonyPatch(typeof(PiggyBankItem), nameof(PiggyBankItem.ActivateSequence))]
    [HarmonyPatch(typeof(MagnifyingGlassItem), nameof(MagnifyingGlassItem.ActivateSequence))]
    [HarmonyPrefix]
    private static bool DisableSequence(ConsumableItem __instance, ref IEnumerator __result)
    {
        if(!Plugin.MultiplayerActive || !MultiplayerRunState.Run.OpponentItemUsed)
            return true;
        __result = BlankActivationSequence(__instance);
        return false;
    }
    
    private static IEnumerator BlankActivationSequence(ConsumableItem item)
    {
        item.PlayExitAnimation();
        yield return new WaitForSeconds(.25f);
    }
}