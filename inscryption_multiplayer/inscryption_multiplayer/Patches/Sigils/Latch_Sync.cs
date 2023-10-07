using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DiskCardGame;
using HarmonyLib;
using static inscryption_multiplayer.Utils;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class Latch_Sync
    {
        private static MethodInfo OnInvalidTargetMethod;
        
        [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.ChooseTarget))]
        [HarmonyPrefix]
        private static void SendLatchTarget(ref Action<CardSlot> targetSelectedCallback, ref Action<CardSlot> invalidTargetCallback)
        {
            if(invalidTargetCallback == null)
                return;
            OnInvalidTargetMethod ??= AccessTools.Method(typeof(Latch), nameof(Latch.OnInvalidTarget));
            if(invalidTargetCallback.Method == OnInvalidTargetMethod)
                targetSelectedCallback += s => {
                    MultiplayerRunState.Run.LatchCommunicator.Send(new CardSlotMultiplayer
                    {
                        isPlayerSlot = !s.IsPlayerSlot,
                        index = s.Index
                    });
                };
        }
        
        [HarmonyPatch(typeof(Latch), nameof(Latch.AISelectTarget))]
        [HarmonyPrefix]
        private static bool ReplaceAISelectTarget(List<CardSlot> validTargets, Action<CardSlot> chosenCallback, ref IEnumerator __result)
        {
            if (!Plugin.MultiplayerActive || validTargets.Count == 0)
                return true;
            __result = WaitForOpponentTarget(validTargets, chosenCallback);
            return false;
        }

        private static IEnumerator WaitForOpponentTarget(List<CardSlot> validTargets, Action<CardSlot> chosenCallback)
        {
            var targetReceiver = MultiplayerRunState.Run.LatchCommunicator.CreateReceiver(waitText: "Waiting for opponent to select target");
            yield return targetReceiver;
            var target = targetReceiver.Data;
            var row = target.isPlayerSlot ? Singleton<BoardManager>.Instance.PlayerSlotsCopy : Singleton<BoardManager>.Instance.OpponentSlotsCopy;
            chosenCallback(row[target.index]);
        }
    }

}
