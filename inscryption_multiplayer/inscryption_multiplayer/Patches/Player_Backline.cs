using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using Newtonsoft.Json;
using Pixelplacement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static inscryption_multiplayer.Utils;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class Player_Backline
    {
        public static CardSlot CardSlotPrefab =
        ResourceBank.Get<CardSlot>("Prefabs/Cards/CardSlot");

        public static readonly Material QueueSlotMaterial =
        ResourceBank.Get<HighlightedInteractable>("Prefabs/Cards/QueueSlot").GetComponentInChildren<MeshRenderer>().material;

        public static readonly Color QueueSlotBaseColor =
        ResourceBank.Get<HighlightedInteractable>("Prefabs/Cards/QueueSlot").defaultColor;

        public static readonly Color QueueSlotInteractColor =
        ResourceBank.Get<HighlightedInteractable>("Prefabs/Cards/QueueSlot").interactableColor;

        public static readonly Color QueueSlotHightlightColor =
        ResourceBank.Get<HighlightedInteractable>("Prefabs/Cards/QueueSlot").highlightedColor;

        public static List<CardSlot> PlayerQueueSlots = new List<CardSlot>();

        public static List<CardSlot> AllPlayerSlots
        {
            get
            {
                List<CardSlot> AllPlayerSlotsPlusQueue = Singleton<BoardManager>.Instance.playerSlots;
                AllPlayerSlotsPlusQueue.AddRange(PlayerQueueSlots);
                return AllPlayerSlotsPlusQueue;
            }
        }

        public static CardSlot GetQueueSlotFromNormalSlot(CardSlot slot)
        {
            foreach (CardSlot queueSlot in PlayerQueueSlots)
            {
                if (queueSlot.opposingSlot == slot)
                {
                    return queueSlot;
                }
            }
            return null;
        }


        public static bool IsPlayerQueueSlot(CardSlot slot)
        {
            return PlayerQueueSlots.Contains(slot);
        }

        private static IEnumerator PatchedSelectSlotForCard(PlayerHand __instance, PlayableCard card)
        {
            __instance.CardsInHand.ForEach(delegate (PlayableCard x)
            {
                x.SetEnabled(false);
            });
            yield return new WaitWhile(() => __instance.ChoosingSlot);
            __instance.OnSelectSlotStartedForCard(card);
            if (Singleton<RuleBookController>.Instance != null)
            {
                Singleton<RuleBookController>.Instance.SetShown(false, true);
            }
            Singleton<BoardManager>.Instance.CancelledSacrifice = false;
            __instance.choosingSlotCard = card;
            if (card != null && card.Anim != null)
            {
                card.Anim.SetSelectedToPlay(true);
            }
            Singleton<BoardManager>.Instance.ShowCardNearBoard(card, true);
            if (Singleton<TurnManager>.Instance.SpecialSequencer != null)
            {
                yield return Singleton<TurnManager>.Instance.SpecialSequencer.CardSelectedFromHand(card);
            }
            bool cardWasPlayed = false;
            bool requiresSacrifices = card.Info.BloodCost > 0;
            if (requiresSacrifices)
            {
                List<CardSlot> validSacrificeSlots = Player_Backline.AllPlayerSlots.FindAll((CardSlot x) => x.Card != null);
                yield return Singleton<BoardManager>.Instance.ChooseSacrificesForCard(validSacrificeSlots, card);
            }
            if (!Singleton<BoardManager>.Instance.CancelledSacrifice)
            {
                List<CardSlot> validSlots2 = PlayerQueueSlots.FindAll((CardSlot x) => x.Card == null);
                yield return Singleton<BoardManager>.Instance.ChooseSlot(validSlots2, !requiresSacrifices);
                CardSlot lastSelectedSlot = Singleton<BoardManager>.Instance.LastSelectedSlot;
                if (lastSelectedSlot != null)
                {
                    cardWasPlayed = true;
                    card.Anim.SetSelectedToPlay(false);

                    if (IsPlayerQueueSlot(lastSelectedSlot))
                    {
                        InscryptionNetworking.Connection.SendJson(NetworkingMessage.CardQueuedByOpponent, CardToMPInfo(card, lastSelectedSlot));
                    }

                    yield return __instance.PlayCardOnSlot(card, lastSelectedSlot);
                    if (card.Info.BonesCost > 0)
                    {
                        yield return Singleton<ResourcesManager>.Instance.SpendBones(card.Info.BonesCost);
                    }
                    if (card.EnergyCost > 0)
                    {
                        yield return Singleton<ResourcesManager>.Instance.SpendEnergy(card.EnergyCost);
                    }
                }
            }
            if (!cardWasPlayed)
            {
                Singleton<BoardManager>.Instance.ShowCardNearBoard(card, false);
            }
            __instance.choosingSlotCard = null;
            if (card != null && card.Anim != null)
            {
                card.Anim.SetSelectedToPlay(false);
            }
            __instance.CardsInHand.ForEach(delegate (PlayableCard x)
            {
                x.SetEnabled(true);
            });
            yield break;
        }

        [HarmonyPatch(typeof(PlayerHand), nameof(PlayerHand.SelectSlotForCard))]
        [HarmonyPrefix]
        public static bool SelectSlotForCardPrefix(ref PlayerHand __instance, PlayableCard card, out IEnumerator __result)
        {
            if (!Plugin.MultiplayerActive)
            {
                __result = null;
                return true;
            }

            __result = PatchedSelectSlotForCard(__instance, card);
            return false;
        }

        public static IEnumerator PatchedAssignCardToSlot(BoardManager __instance, PlayableCard card, CardSlot AssignedSlot, float transitionDuration = 0.1f, Action tweenCompleteCallback = null, bool resolveTriggers = true)
        {
            CardSlot slot2 = card.Slot;
            if (card.Slot != null)
            {
                card.Slot.Card = null;
            }
            if (AssignedSlot.Card != null)
            {
                AssignedSlot.Card.Slot = null;
            }
            card.SetEnabled(false);
            AssignedSlot.Card = card;

            if (!IsPlayerQueueSlot(AssignedSlot))
            {
                card.Slot = AssignedSlot;
            }
            else
            {

                //otherCard.QueuedSlot = Singleton<BoardManager>.Instance.playerSlots[Game_Patches.PlayerQueueSlots.IndexOf(otherCard.slot)];
                AssignedSlot.Card = card;
                card.Slot = AssignedSlot;
                card.OpponentCard = false;
                InscryptionNetworking.Connection.SendJson(NetworkingMessage.CardQueuedByOpponent, CardToMPInfo(card));
            }

            card.RenderCard();
            if (!AssignedSlot.IsPlayerSlot && !IsPlayerQueueSlot(AssignedSlot))
            {
                card.SetIsOpponentCard(true);
            }
            card.transform.parent = AssignedSlot.transform;
            card.Anim.PlayRiffleSound();
            Tween.LocalPosition(card.transform, Vector3.up * (__instance.SlotHeightOffset + card.SlotHeightOffset), transitionDuration, 0.05f, Tween.EaseOut, Tween.LoopType.None, null, delegate ()
            {
                Action tweenCompleteCallback2 = tweenCompleteCallback;
                if (tweenCompleteCallback2 != null)
                {
                    tweenCompleteCallback2();
                }
                card.Anim.PlayRiffleSound();
            }, true);

            Tween.Rotation(card.transform, AssignedSlot.transform.GetChild(0).rotation, transitionDuration, 0f, Tween.EaseOut, Tween.LoopType.None, null, null, true);

            if (!IsPlayerQueueSlot(AssignedSlot))
            {
                if (resolveTriggers && slot2 != card.Slot)
                {
                    yield return Singleton<GlobalTriggerHandler>.Instance.TriggerCardsOnBoard(Trigger.OtherCardAssignedToSlot, false, new object[]
                    {
                    card
                    });
                }
            }
            yield break;
        }

        [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.AssignCardToSlot))]
        [HarmonyPrefix]
        public static bool AssignCardToSlotPrefix(ref BoardManager __instance, out IEnumerator __result, PlayableCard card, CardSlot slot, float transitionDuration = 0.1f, Action tweenCompleteCallback = null, bool resolveTriggers = true)
        {
            if (!Plugin.MultiplayerActive)
            {
                __result = null;
                return true;
            }

            __result = PatchedAssignCardToSlot(__instance, card, slot, transitionDuration, tweenCompleteCallback, resolveTriggers);
            return false;
        }

        public static IEnumerator PatchedResolveCardOnBoard(BoardManager __instance, PlayableCard card, CardSlot slot, float tweenLength = 0.1f, Action landOnBoardCallback = null, bool resolveTriggers = true)
        {
            yield return __instance.AssignCardToSlot(card, slot, tweenLength, delegate
            {
                if (!card.OpponentCard)
                {
                    card.Anim.PlayLandOnBoardEffects();
                }
                Action landOnBoardCallback2 = landOnBoardCallback;
                if (landOnBoardCallback2 != null)
                {
                    landOnBoardCallback2();
                }
                card.OnPlayed();
            }, resolveTriggers);
            if (resolveTriggers && !IsPlayerQueueSlot(card.Slot))
            {
                if (card.TriggerHandler.RespondsToTrigger(Trigger.ResolveOnBoard, Array.Empty<object>()))
                {
                    yield return card.TriggerHandler.OnTrigger(Trigger.ResolveOnBoard, Array.Empty<object>());
                }
                yield return Singleton<GlobalTriggerHandler>.Instance.TriggerCardsOnBoard(Trigger.OtherCardResolve, false, new object[]
                {
                    card
                });
            }
            if (Singleton<TurnManager>.Instance.IsPlayerTurn)
            {
                __instance.playerCardsPlayedThisRound.Add(card.Info);
            }
            yield break;
        }

        [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.ResolveCardOnBoard))]
        [HarmonyPrefix]
        public static bool ResolveCardOnBoardPrefix(ref BoardManager __instance, out IEnumerator __result, PlayableCard card, CardSlot slot, float tweenLength = 0.1f, Action landOnBoardCallback = null, bool resolveTriggers = true)
        {
            if (!Plugin.MultiplayerActive)
            {
                __result = null;
                return true;
            }

            __result = PatchedResolveCardOnBoard(__instance, card, slot, tweenLength, landOnBoardCallback, resolveTriggers);
            return false;
        }

        [HarmonyPatch(typeof(BoardManager), nameof(BoardManager.AvailableSacrificeValue), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AvailableSacrificeValuePostfix(ref BoardManager __instance, ref int __result)
        {
            if (!Plugin.MultiplayerActive)
            {
                return;
            }

            __result = __instance.GetValueOfSacrifices(Player_Backline.AllPlayerSlots.FindAll((CardSlot x) => x.Card != null && x.Card.CanBeSacrificed));
        }

        [HarmonyPatch(typeof(CardSlot), nameof(CardSlot.Index), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IndexPrefix(ref CardSlot __instance, ref int __result)
        {
            if (!Plugin.MultiplayerActive)
            {
                return true;
            }

            if (IsPlayerQueueSlot(__instance))
            {
                __result = PlayerQueueSlots.IndexOf(__instance);
            }
            else
            {
                if (__instance.IsPlayerSlot)
                {
                    __result = Singleton<BoardManager>.Instance.PlayerSlotsCopy.IndexOf(__instance);
                }
                else
                {
                    __result = Singleton<BoardManager>.Instance.OpponentSlotsCopy.IndexOf(__instance);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(CardSlot), nameof(CardSlot.IsPlayerSlot), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IsPlayerSlotPrefix(ref CardSlot __instance, ref bool __result)
        {
            if (!Plugin.MultiplayerActive)
            {
                return true;
            }

            if (IsPlayerQueueSlot(__instance))
            {
                __result = true;
            }
            else
            {
                __result = Singleton<BoardManager>.Instance.PlayerSlotsCopy.Contains(__instance);
            }
            return false;
        }

        [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.DealOverkillDamage))]
        [HarmonyPrefix]
        public static bool DealOverkillDamagePrefix(ref int damage, ref CardSlot attackingSlot, ref CardSlot opposingSlot, ref CombatPhaseManager __instance, out IEnumerator __result)
        {
            if (!Plugin.MultiplayerActive)
            {
                __result = null;
                return true;
            }

            if (attackingSlot.Card != null && !attackingSlot.IsPlayerSlot && damage > 0)
            {
                CardSlot queueSlot = GetQueueSlotFromNormalSlot(opposingSlot);
                if (queueSlot?.Card != null)
                {
                    __result = PatchedDealOverkillDamage(queueSlot.Card, damage, attackingSlot, __instance);
                    return false;
                }
            }
            __result = null;
            return true;
        }

        public static IEnumerator PatchedDealOverkillDamage(PlayableCard queuedCard, int damage, CardSlot attackingSlot, CombatPhaseManager __instance)
        {
            yield return new WaitForSeconds(0.1f);
            Singleton<ViewManager>.Instance.SwitchToView(Singleton<BoardManager>.Instance.QueueView, false, false);
            yield return new WaitForSeconds(0.3f);
            if (queuedCard.HasAbility(Ability.PreventAttack))
            {
                yield return __instance.ShowCardBlocked(attackingSlot.Card);
            }
            else
            {
                yield return __instance.PreOverkillDamage(queuedCard);
                yield return queuedCard.TakeDamage(damage, attackingSlot.Card);
                yield return __instance.PostOverkillDamage(queuedCard);
            }
        }

        [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.GetViewInfo))]
        [HarmonyPostfix]
        public static void Postfix(View view, ref ViewInfo __result)
        {
            if (!Plugin.MultiplayerActive)
            {
                return;
            }

            ViewInfo viewInfo = new ViewInfo();
            switch (view)
            {
                //case View.Board:
                //    viewInfo.camPosition = new Vector3(0.475f, 9.71f, -3.36f);
                //    viewInfo.handPosition = PlayerHand3D.DEFAULT_HAND_POS;
                //    viewInfo.camRotation = new Vector3(79.9f, 0f, 0f);
                //    viewInfo.fov = 50f;
                //    break;
                //case View.OpponentQueue:
                //    viewInfo.camPosition = new Vector3(0.475f, 9.71f, -2.86f);
                //    viewInfo.camRotation = new Vector3(60f, 0f, 0f);
                //    viewInfo.fov = 55f;
                //    break;
                //default:
                //    return;

                case View.Board:
                    viewInfo.camPosition = new Vector3(0.475f, 9.71f, -4.86f);
                    viewInfo.camRotation = new Vector3(60f, 0f, 0f);
                    viewInfo.fov = 55f;
                    break;
                case View.OpponentQueue:
                    viewInfo.camPosition = new Vector3(0.475f, 9.71f, -2.86f);
                    viewInfo.camRotation = new Vector3(60f, 0f, 0f);
                    viewInfo.fov = 55f;
                    break;
                default:
                    return;

            }
            __result = viewInfo;
        }

        [HarmonyPatch(typeof(GlobalTriggerHandler), nameof(GlobalTriggerHandler.TriggerCardsOnBoard))]
        [HarmonyPrefix]
        public static bool TriggerCardsOnBoardPrefix(ref GlobalTriggerHandler __instance, out IEnumerator __result, Trigger trigger, bool triggerFacedown, params object[] otherArgs)
        {
            if (!Plugin.MultiplayerActive)
            {
                __result = null;
                return true;
            }

            __result = PatchedTriggerCardsOnBoard(__instance, trigger, triggerFacedown, otherArgs);
            return false;
        }

        public static IEnumerator PatchedTriggerCardsOnBoard(GlobalTriggerHandler __instance, Trigger trigger, bool triggerFacedown, params object[] otherArgs)
        {
            yield return __instance.TriggerNonCardReceivers(true, trigger, otherArgs);
            List<PlayableCard> list = new List<PlayableCard>(Singleton<BoardManager>.Instance.CardsOnBoard);
            foreach (PlayableCard playableCard in list)
            {
                if (playableCard != null && (!playableCard.FaceDown || triggerFacedown) && !IsPlayerQueueSlot(playableCard.Slot) && playableCard.TriggerHandler.RespondsToTrigger(trigger, otherArgs))
                {
                    yield return playableCard.TriggerHandler.OnTrigger(trigger, otherArgs);
                }
            }
            List<PlayableCard>.Enumerator enumerator = default(List<PlayableCard>.Enumerator);
            yield return __instance.TriggerNonCardReceivers(false, trigger, otherArgs);
            yield break;
        }

        [HarmonyPatch(typeof(BoardManager3D), nameof(BoardManager3D.Initialize))]
        [HarmonyPrefix]
        public static void AddPlayerBackline(BoardManager3D __instance)
        {
            if (!Plugin.MultiplayerActive)
            {
                return;
            }

            if (PlayerQueueSlots.Count == 0)
            {

                for (int i = 0; i < Singleton<BoardManager>.Instance.AllSlotsCopy.Count; i++)
                {
                    Vector3 NewPos = Singleton<BoardManager>.Instance.AllSlots[i].transform.position + new Vector3(0, 0, 0.25f);
                    Singleton<BoardManager>.Instance.AllSlots[i].transform.position = NewPos;
                }

                for (int i = 0; i < Singleton<BoardManager>.Instance.OpponentQueueSlots.Count; i++)
                {
                    Vector3 NewPos = Singleton<BoardManager>.Instance.OpponentQueueSlots[i].transform.position + new Vector3(0, 0, 0.25f);
                    Singleton<BoardManager>.Instance.OpponentQueueSlots[i].transform.position = NewPos;
                }

                Transform BoardObject = Singleton<BoardManager3D>.Instance.transform;
                Transform playerSlotsObject = BoardObject.Find("PlayerSlots");

                BoardObject.Find("CardDrawPiles").transform.localPosition = new Vector3(0.75f, 0f, 0f);

                foreach (CardSlot slot in Singleton<BoardManager>.Instance.playerSlots)
                {
                    CardSlot PlayerBackline = UnityEngine.Object.Instantiate(CardSlotPrefab, playerSlotsObject);
                    PlayerBackline.transform.position = slot.gameObject.transform.position + new Vector3(0, 0, -2.1f);
                    PlayerBackline.transform.GetChild(0).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Material PlayerQueueSlotMaterial = QueueSlotMaterial;
                    PlayerQueueSlotMaterial.mainTextureScale = new Vector2(1, -1);
                    PlayerBackline.transform.GetComponentInChildren<MeshRenderer>().material = PlayerQueueSlotMaterial;
                    PlayerBackline.currentState = HighlightedInteractable.State.Interactable;
                    PlayerBackline.SetColors(QueueSlotBaseColor, QueueSlotInteractColor, QueueSlotHightlightColor);
                    PlayerBackline.opposingSlot = slot;

                    PlayerQueueSlots.Add(PlayerBackline);
                }
            }
            else
            {
                foreach (CardSlot slot in PlayerQueueSlots)
                {
                    slot.gameObject.SetActive(true);
                }
            }
        }
    }
}
