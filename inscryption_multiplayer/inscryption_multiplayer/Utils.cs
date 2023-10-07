using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace inscryption_multiplayer
{
    public class Utils
    {
        public static IEnumerator CallbackRoutine(IEnumerator coroutine, Action callback)
        {
            yield return coroutine;
            callback();
        }

        public static IEnumerator JoinCoroutines(params IEnumerator[] coroutines)
        {
            foreach (var coroutine in coroutines)
                yield return coroutine;
        }
        public class CardInfoMultiplayer
        {
            public string name { get; set; }
            public List<CardModificationInfo> mods { get; set; }
            public List<CardModificationInfo> temporaryMods { get; set; }
            public CardSlotMultiplayer slot { get; set; }
        }
        public class CardSlotMultiplayer
        {
            public bool isPlayerSlot { get; set; }
            public int index { get; set; }
            public bool isQueueSlot { get; set; }
        }

        public static CardInfoMultiplayer CardToMPInfo(PlayableCard card, CardSlot slotOverride = null, bool invertSide = true)
        {
            CardSlot slot = slotOverride ?? card.Slot;

            CardInfoMultiplayer cardInfo = new CardInfoMultiplayer
            {
                temporaryMods = card.TemporaryMods,
                mods = card.Info.Mods,
                name = card.Info.name,
                slot = new CardSlotMultiplayer
                {
                    index = slot.Index,
                    isPlayerSlot = !slot.IsPlayerSlot == invertSide
                }
            };
            return cardInfo;
        }

        public static CardSlotMultiplayer SlotToMPInfo(CardSlot slot, bool invertSide = true)
        {
            CardSlotMultiplayer slotInfo = new CardSlotMultiplayer
            {
                index = slot.Index,
                isPlayerSlot = !slot.IsPlayerSlot == invertSide,
                isQueueSlot = Player_Backline.IsPlayerQueueSlot(slot)
            };
            return slotInfo;
        }

        public static PlayableCard MPInfoToCard(CardInfoMultiplayer info, string singletonId = null)
        {
            CardInfo cardInfo = CardLoader.GetCardByName(info.name);
            cardInfo.Mods = info.mods ?? new List<CardModificationInfo>();

            PlayableCard playableCard = CardSpawner.SpawnPlayableCard(cardInfo);
            playableCard.TemporaryMods = info.temporaryMods ?? new List<CardModificationInfo>();

            if (!info.slot.isPlayerSlot)
            {
                playableCard.SetIsOpponentCard(true);
                Singleton<TurnManager>.Instance.Opponent.ModifySpawnedCard(playableCard);
            }
            return playableCard;
        }

        public static CardModificationInfo GetModWithSingletonId(PlayableCard card, string singletonId)
        {
            foreach (CardModificationInfo mod in card.TemporaryMods)
            {
                if (mod.singletonId == singletonId)
                {
                    return mod;
                }
            }
            return null;
        }

        public static CardSlot MPInfoToSlot(CardSlotMultiplayer info)
        {
            return Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == info.index && x.IsPlayerSlot == info.isPlayerSlot);
        }

        public static IEnumerator PlayCardsInPlayerQueue(float tweenLength = 0.1f)
        {
            if (Player_Backline.PlayerQueueSlots.Count > 0)
            {
                List<PlayableCard> queuedCards = Player_Backline.PlayerQueueSlots.Where(x => x.Card != null && !x.Card.Dead).Select(x => x.Card).ToList();
                if (queuedCards.Exists((PlayableCard x) => !PlayerQueuedCardIsBlocked(x)))
                {
                    Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, false);
                    yield return new WaitForSeconds(0.15f);

                    List<PlayableCard> playedCards = new List<PlayableCard>();
                    queuedCards.Sort((PlayableCard a, PlayableCard b) => a.QueuedSlot.Index - b.QueuedSlot.Index);
                    foreach (PlayableCard queuedCard in queuedCards)
                    {
                        if (!PlayerQueuedCardIsBlocked(queuedCard))
                        {
                            CardSlot queuedSlot = queuedCard.Slot.opposingSlot;
                            yield return Singleton<BoardManager>.Instance.ResolveCardOnBoard(queuedCard, queuedSlot, tweenLength, null, true);
                            playedCards.Add(queuedCard);
                        }
                    }
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield break;
        }

        public static bool PlayerQueuedCardIsBlocked(PlayableCard queuedCard)
        {
            return queuedCard.Slot.opposingSlot.Card != null;
        }

        public static IEnumerator PlayCardsInOpponentQueue(float tweenLength = 0.1f)
        {
            Opponent opponent = Singleton<Opponent>.Instance;
            if (opponent.Queue.Count > 0)
            {
                List<PlayableCard> queuedCards = new List<PlayableCard>(opponent.Queue);
                if (queuedCards.Exists((PlayableCard x) => !opponent.QueuedCardIsBlocked(x)))
                {
                    yield return opponent.VisualizePrePlayQueuedCards();

                    Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, false);
                    yield return new WaitForSeconds(0.15f);

                    List<PlayableCard> playedCards = new List<PlayableCard>();
                    queuedCards.Sort((PlayableCard a, PlayableCard b) => a.QueuedSlot.Index - b.QueuedSlot.Index);
                    foreach (PlayableCard queuedCard in queuedCards)
                    {
                        if (!opponent.QueuedCardIsBlocked(queuedCard))
                        {
                            CardSlot queuedSlot = queuedCard.QueuedSlot;
                            queuedCard.QueuedSlot = null;
                            if (queuedCard != null)
                            {
                                queuedCard.OnPlayedFromOpponentQueue();
                            }
                            yield return Singleton<BoardManager>.Instance.ResolveCardOnBoard(queuedCard, queuedSlot, tweenLength, null, true);
                            playedCards.Add(queuedCard);
                        }
                    }
                    opponent.Queue.RemoveAll((PlayableCard x) => playedCards.Contains(x));
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield break;
        }
    }
}
