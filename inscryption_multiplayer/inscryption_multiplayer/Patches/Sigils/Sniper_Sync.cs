using System.Collections;
using System.Collections.Generic;
using DiskCardGame;
using HarmonyLib;
using InscryptionCommunityPatch.Card;
using UnityEngine;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class Sniper_Sync
    {
        [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.VisualizeConfirmSniperAbility))]
        [HarmonyPrefix]
        private static void SendSniperTarget(CardSlot targetSlot)
        {
            if (Plugin.MultiplayerActive && !targetSlot.IsPlayerSlot)
                MultiplayerRunState.Run.SniperCommunicator.Send(targetSlot.Index);
        }
        
        [HarmonyPatch(typeof(SniperFix), nameof(SniperFix.OpponentSniperLogic))]
        [HarmonyPrefix]
        private static bool ReplaceOpponentSniperLogic(CombatPhaseManager instance, Part1SniperVisualizer visualizer, List<CardSlot> opposingSlots, int numAttacks, ref IEnumerator __result)
        {
            if (!Plugin.MultiplayerActive)
                return true;
            __result = WaitForOpponentTarget(instance, visualizer, opposingSlots, numAttacks);
            return false;
        }

        private static IEnumerator WaitForOpponentTarget(CombatPhaseManager instance, Part1SniperVisualizer visualizer, List<CardSlot> opposingSlots, int numAttacks)
        {
            for (int i = 0; i < numAttacks; i++)
            {
                var receiver = MultiplayerRunState.Run.SniperCommunicator.CreateReceiver(waitText: "Waiting for opponent to select target");
                yield return receiver;
                var slot = Singleton<BoardManager>.Instance.PlayerSlotsCopy[receiver.Data];
                opposingSlots.Add(slot);
                instance.VisualizeConfirmSniperAbility(slot);
                visualizer?.VisualizeConfirmSniperAbility(slot);
                yield return new WaitForSeconds(.25f);
            }
        }
    }
}