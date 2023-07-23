using System.Reflection.Emit;
using System.Collections.Generic;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;

[HarmonyPatch]
public class Pliers_Sync
{
    public static bool PlayerDamage => Plugin.MultiplayerActive && MultiplayerRunState.Run.OpponentItemUsed;
    
    [HarmonyPatch(typeof(PliersItem), nameof(PliersItem.ActivateSequence), MethodType.Enumerator)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> GiveDamageToPlayer(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_1), new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Ldc_R4, .25f))
            .Advance(2)
            .SetInstruction(new CodeInstruction(OpCodes.Call,
                AccessTools.PropertyGetter(typeof(Pliers_Sync), nameof(PlayerDamage))))
            .InstructionEnumeration();
    }
}