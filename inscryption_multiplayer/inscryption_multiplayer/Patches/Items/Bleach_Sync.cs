using System.Collections.Generic;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;

[HarmonyPatch]
public class Bleach_Sync
{
    [HarmonyPatch(typeof(BleachPotItem), nameof(BleachPotItem.ActivateSequence), MethodType.Enumerator)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> GetPlayerSlots(IEnumerable<CodeInstruction> instructions)
    {
        var boardManagerInstanceGetter = AccessTools.PropertyGetter(typeof(Singleton<BoardManager>), nameof(Singleton<BoardManager>.Instance));
        return new CodeMatcher(instructions)
            .MatchForward(true, new CodeMatch(OpCodes.Call, boardManagerInstanceGetter), new CodeMatch(OpCodes.Ldc_I4_0))
            .SetOperandAndAdvance(OpCodes.Ldc_I4_1)
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(BleachPotItem), nameof(BleachPotItem.GetValidOpponentSlots))]
    [HarmonyPrefix]
    public static bool GetValidSlots(BleachPotItem __instance, ref List<CardSlot> __result)
    {
        if(!Plugin.MultiplayerActive || !MultiplayerRunState.Run.OpponentItemUsed)
            return true;
        __result = Singleton<BoardManager>.Instance.PlayerSlotsCopy
            .FindAll(x => x.Card != null && !__instance.CardHasNoAbilities(x.Card));
        return false;
    }
}