using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;
using UnityEngine;

[HarmonyPatch]
public class Knife_Sync
{
    [HarmonyPatch(typeof(TrapperKnifeItem), nameof(TrapperKnifeItem.OnValidTargetSelected), MethodType.Enumerator)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DisableCardSpawn(IEnumerable<CodeInstruction> instructions)
    {
        var cardSpawnerMethod = AccessTools.Method(typeof(CardSpawner), nameof(CardSpawner.SpawnCardToHand), new Type[] {typeof(CardInfo), typeof(float)});
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(cardSpawnerMethod))
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(Knife_Sync), nameof(PatchedCardSpawn)));
            else yield return instruction;
        }
    }

    private static IEnumerator PatchedCardSpawn(CardSpawner spawner, CardInfo cardInfo, float waitTime)
    {
        if (!Plugin.MultiplayerActive || !MultiplayerRunState.Run.OpponentItemUsed)
            yield return spawner.SpawnCardToHand(cardInfo, waitTime);
        else yield return new WaitForSeconds(.25f);
    }
}
