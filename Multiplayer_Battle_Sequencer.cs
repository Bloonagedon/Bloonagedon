using inscryption_multiplayer;
using inscryption_multiplayer.Networking;
using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace DiskCardGame
{
    public class Multiplayer_Battle_Sequencer : SpecialBattleSequencer
    {
        public bool OpponentCardPlacePhase = false;
        public bool OpponentReady = false;
        public override IEnumerator OpponentUpkeep()
        {
            OpponentCardPlacePhase = true;
            string transformedMessage = Singleton<TextDisplayer>.Instance.ShowMessage("waiting for opponent to finish their turn");
            if (!string.IsNullOrEmpty(transformedMessage))
            {
                Singleton<TextDisplayer>.Instance.CurrentAdvanceMode = TextDisplayer.MessageAdvanceMode.Auto;
                yield return new WaitUntil(() => OpponentCardPlacePhase == false);
                if (Singleton<TextDisplayer>.Instance.textMesh.text == transformedMessage)
                {
                    Singleton<TextDisplayer>.Instance.Clear();
                }
            }
            yield break;
        }

        public override EncounterData BuildCustomEncounter(CardBattleNodeData nodeData)
        {
            return new EncounterData();
        }

        public override IEnumerator PlayerCombatStart()
        {
            InscryptionNetworking.Connection.Send("OpponentCardPlacePhaseEnded");
            yield break;
        }

        public override IEnumerator PreHandDraw()
        {
            //i assign the trigger handler to a slot, there might be something better to attach it to but this seems the easiest for now
            GlobalTriggerHandlerMultiplayer triggerHandler = Singleton<BoardManager>.Instance.AllSlots[0].gameObject.AddComponent<GlobalTriggerHandlerMultiplayer>();
            Singleton<GlobalTriggerHandler>.Instance.RegisterNonCardReceiver(triggerHandler);

            InscryptionNetworking.Connection.Send("OpponentReady");
            if (!OpponentReady)
            {
                string transformedMessage = Singleton<TextDisplayer>.Instance.ShowMessage("waiting for opponent");
                if (!string.IsNullOrEmpty(transformedMessage))
                {
                    Singleton<TextDisplayer>.Instance.CurrentAdvanceMode = TextDisplayer.MessageAdvanceMode.Auto;
                    yield return new WaitUntil(() => OpponentReady == true);
                    if (Singleton<TextDisplayer>.Instance.textMesh.text == transformedMessage)
                    {
                        Singleton<TextDisplayer>.Instance.Clear();
                    }
                }
            }
            OpponentReady = false;

            if (InscryptionNetworking.Connection.IsHost && !SteamNetworking.START_ALONE)
            {
                Random rand = new Random();
                if (rand.Next(0, 2) != 0)
                {
                    Singleton<TurnManager>.Instance.playerInitiatedCombat = true;
                }
                else
                {
                    InscryptionNetworking.Connection.Send("InitiateCombat");
                }
            }
            yield break;
        }
    }
}
