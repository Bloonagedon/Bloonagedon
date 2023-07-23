using System.Collections.Generic;
using HarmonyLib;
using DiskCardGame;
using inscryption_multiplayer;

[HarmonyPatch]
public class Fan_Sync
{
    [HarmonyPatch(typeof(BirdLegFanItem), nameof(BirdLegFanItem.GetValidTargets))]
    [HarmonyPrefix]
    public static bool ValidTargets(ref List<PlayableCard> __result)
    {
        if(!Plugin.MultiplayerActive || ! MultiplayerRunState.Run.OpponentItemUsed)
            return true;
        __result = Singleton<BoardManager>.Instance.CardsOnBoard.FindAll(x => x.OpponentCard && !x.HasAbility(Ability.Flying));
        return false;
    }
}