using inscryption_multiplayer;
using inscryption_multiplayer.Networking;
using System.Collections;
using UnityEngine;

namespace DiskCardGame
{
    public class Multiplayer_Battle_Sequencer : SpecialBattleSequencer
    {
        public bool OpponentCardPlacePhase = false;
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
            yield break;
        }
    }
}
