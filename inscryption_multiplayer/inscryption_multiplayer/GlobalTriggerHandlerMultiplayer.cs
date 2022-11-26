using DiskCardGame;
using inscryption_multiplayer.Networking;
using System.Collections;
using System.Collections.Generic;

namespace inscryption_multiplayer
{
    public class CardInfoMultiplayer
    {
        public List<CardModificationInfo> mods { get; set; }
        public string name { get; set; }
        public CardSlotMultiplayer slot { get; set; }
    }
    public class CardSlotMultiplayer
    {
        public bool isPlayerSlot { get; set; }
        public int index { get; set; }
    }

    public class GlobalTriggerHandlerMultiplayer : NonCardTriggerReceiver
    {
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
                mods = otherCard.Info.mods,
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
                isPlayerSlot = !deathSlot.IsPlayerSlot
            };
            InscryptionNetworking.Connection.SendJson("CardSacrificedByOpponent", cardSlot);
            yield break;
        }
    }
}
