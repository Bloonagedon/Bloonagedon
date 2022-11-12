using DiskCardGame;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Game_Patches
    {
        private static IEnumerator PatchedOpponentTurn(TurnManager __instance)
        {
            __instance.IsPlayerTurn = false;
            if (Singleton<PlayerHand>.Instance != null)
            {
                Singleton<PlayerHand>.Instance.PlayingLocked = true;
            }
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;
            if (__instance.Opponent.SkipNextTurn)
            {
                yield return Singleton<TextDisplayer>.Instance.PlayDialogueEvent("OpponentSkipTurn", TextDisplayer.MessageAdvanceMode.Auto, TextDisplayer.EventIntersectMode.Wait, null, null);
                __instance.Opponent.SkipNextTurn = false;
            }
            else
            {
                yield return __instance.DoUpkeepPhase(false);

                //check if the battle is a multiplayer battle and if so skip the card placements for the opponent
                if (__instance.SpecialSequencer is not Multiplayer_Battle_Sequencer)
                {
                    yield return __instance.opponent.PlayCardsInQueue(0.1f);
                    yield return __instance.opponent.QueueNewCards(true, true);
                }

                yield return __instance.DoCombatPhase(false);
                if (__instance.LifeLossConditionsMet())
                {
                    yield return new WaitForSeconds(0.5f);
                    yield break;
                }
                yield return Singleton<GlobalTriggerHandler>.Instance.TriggerCardsOnBoard(Trigger.TurnEnd, true, new object[]
                {
                    false
                });
            }
        }
        
        [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.OpponentTurn))]
        [HarmonyPrefix]
        public static bool Prefix(ref TurnManager __instance, out IEnumerator __result)
        {
            __result = PatchedOpponentTurn(__instance);
            return false;
        }
    }
}
