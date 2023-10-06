using DiskCardGame;
using GBC;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using System.Collections;
using UnityEngine;
using static inscryption_multiplayer.Utils;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Game_Patches
    {
        public static bool OpponentReady = false;

        private static IEnumerator PatchedOpponentTurn(TurnManager __instance)
        {
            __instance.IsPlayerTurn = false;
            Singleton<ViewManager>.Instance.SwitchToView(View.OpponentQueue);
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
            Singleton<ViewManager>.Instance.SwitchToView(View.Board);
        }

        [HarmonyPatch(typeof(LifeManager), nameof(LifeManager.Initialize))]
        [HarmonyPostfix]
        public static void WaitForOpponent(ref IEnumerator __result)
        {
            if (Plugin.MultiplayerActive)
                __result = Utils.JoinCoroutines(__result, Utils.CallbackRoutine(Multiplayer_Battle_Sequencer.WaitForOpponent(), Totem_Sync.ApplyTotem));
        }


        [HarmonyPatch(typeof(TurnManager), nameof(TurnManager.OpponentTurn))]
        [HarmonyPrefix]
        public static bool Prefix(ref TurnManager __instance, ref IEnumerator __result)
        {
            if (Plugin.MultiplayerActive)
            {
                __result = PatchedOpponentTurn(__instance);
                return false;
            }
            return true;
        }

        //probably gotta find a less janky solution, but i'm tired so this is good enough for now
        [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.SlotAttackSequence))]
        [HarmonyPrefix]
        public static bool SlotAttackSequencePrefix(CardSlot slot)
        {
            if (Plugin.MultiplayerActive && Player_Backline.IsPlayerQueueSlot(slot))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Part1GameFlowManager), nameof(Part1GameFlowManager.PlayerLostBattleSequence))]
        [HarmonyPrefix]
        public static bool PlayerLostBattleSequencePrefix()
        {
            return !Plugin.MultiplayerActive;
        }

        [HarmonyPatch(typeof(CardDrawPiles), nameof(CardDrawPiles.ExhaustedSequence))]
        [HarmonyPrefix]
        public static bool ExhaustedSequencePrefix()
        {
            return !Plugin.MultiplayerActive;
        }

        [HarmonyPatch(typeof(OptionsUI), nameof(OptionsUI.OnRunInBackgroundToggled))]
        [HarmonyPrefix]
        public static bool OnRunInBackgroundToggledPrefix(bool on)
        {
            if (Plugin.MultiplayerActive)
            {
                GameOptions.Options.runInBackground = on;
                Application.runInBackground = true;
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(ConsumableItem), nameof(ConsumableItem.ConsumablesCanBeActivated))]
        [HarmonyPrefix]
        public static bool ConsumablesCanBeActivatedPrefix(ref bool __result)
        {
            if (Plugin.MultiplayerActive && !GameSettings.Current.AllowItems)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.DealOverkillDamage))]
        [HarmonyPrefix]
        public static bool DealOverkillDamagePrefix()
        {
            if (Plugin.MultiplayerActive && !GameSettings.Current.AllowBackrows)
            {
                return false;
            }
            return true;
        }
    }
}
