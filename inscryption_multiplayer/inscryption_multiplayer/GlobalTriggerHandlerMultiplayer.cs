using DiskCardGame;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace inscryption_multiplayer
{
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

    public class GlobalTriggerHandlerMultiplayer : NonCardTriggerReceiver
    {
        private static readonly CardInfoMultiplayer _TestCardInfo = new()
        {
            name = "Boulder",
            mods = new List<CardModificationInfo>
            {
                new CardModificationInfo(Ability.Brittle),
                new CardModificationInfo(1, 0)
            },
            slot = new CardSlotMultiplayer
            {
                isPlayerSlot = false
            }
        };

        public static CardInfoMultiplayer TestCardInfo
        {
            get
            {
                _TestCardInfo.slot.index = Random.Range(0, 4);
                return _TestCardInfo;
            }
        }

        public static CardInfoMultiplayer TestCardInfoWithSpecificSlot(int index)
        {
            _TestCardInfo.slot.index = index;
            return _TestCardInfo;
        }

        public override bool RespondsToOtherCardResolve(PlayableCard otherCard)
        {
            //not needed because i can disable triggers when placing a card but i just left it here as it might be useful in the future
            //return otherCard.temporaryMods.Any(x => x.singletonId != "PlacedByMultiplayerOpponent");

            return true;
        }

        public override IEnumerator OnOtherCardResolve(PlayableCard otherCard)
        {
            CardInfoMultiplayer cardInfo = new CardInfoMultiplayer
            {
                temporaryMods = otherCard.TemporaryMods,
                mods = otherCard.Info.Mods,
                name = otherCard.Info.name,
                slot = new CardSlotMultiplayer
                {
                    index = otherCard.Slot.Index,
                    isPlayerSlot = !otherCard.Slot.IsPlayerSlot
                }
            };
            InscryptionNetworking.Connection.SendJson("CardPlacedByOpponent", cardInfo);
            yield break;
        }

        public override bool RespondsToOtherCardDie(PlayableCard card, CardSlot deathSlot, bool fromCombat, PlayableCard killer)
        {
            return !fromCombat;
        }

        public override IEnumerator OnOtherCardDie(PlayableCard card, CardSlot deathSlot, bool fromCombat, PlayableCard killer)
        {
            CardSlotMultiplayer cardSlot = new CardSlotMultiplayer
            {
                index = deathSlot.Index,
                isPlayerSlot = !deathSlot.IsPlayerSlot,
                isQueueSlot = Player_Backline.IsPlayerQueueSlot(deathSlot)
            };
            InscryptionNetworking.Connection.SendJson("CardSacrificedByOpponent", cardSlot);
            yield break;
        }
    }
}
