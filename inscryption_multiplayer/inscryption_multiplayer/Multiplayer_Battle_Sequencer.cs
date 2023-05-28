using inscryption_multiplayer;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using System.Collections;
using UnityEngine;
using Random = System.Random;

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
                if (InscryptionNetworking.Connection.PlayAgainstBot)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    InscryptionNetworking.Connection.SendJson("bypasscheck CardPlacedByOpponent",
                        GlobalTriggerHandlerMultiplayer.TestCardInfo);
                }
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

        public override IEnumerator PlayerUpkeep()
        {
            Singleton<ViewManager>.Instance.SwitchToView(View.Board, false, true);
            for (int i = 0; i < Player_Backline.PlayerQueueSlots.Count; i++)
            {
                PlayableCard card = Player_Backline.PlayerQueueSlots[i].Card;
                CardSlot newSlot = Singleton<BoardManager>.Instance.playerSlots[i];
                if (card != null && newSlot.Card == null)
                {
                    yield return Singleton<BoardManager>.Instance.ResolveCardOnBoard(card, newSlot);
                }
            }
            Singleton<ViewManager>.Instance.SwitchToView(View.Hand);
            yield break;
        }

        public override IEnumerator PreHandDraw()
        {
            //i assign the trigger handler to a slot, there might be something better to attach it to but this seems the easiest for now
            GlobalTriggerHandlerMultiplayer triggerHandler = Singleton<BoardManager>.Instance.AllSlots[0].gameObject.AddComponent<GlobalTriggerHandlerMultiplayer>();
            Singleton<GlobalTriggerHandler>.Instance.RegisterNonCardReceiver(triggerHandler);

            InscryptionNetworking.Connection.Send("bypasscheck OpponentReady");
            if (!Game_Patches.OpponentReady)
            {
                string transformedMessage = Singleton<TextDisplayer>.Instance.ShowMessage("waiting for opponent");
                if (!string.IsNullOrEmpty(transformedMessage))
                {
                    Singleton<TextDisplayer>.Instance.CurrentAdvanceMode = TextDisplayer.MessageAdvanceMode.Auto;
                    yield return new WaitUntil(() => Game_Patches.OpponentReady);
                    if (Singleton<TextDisplayer>.Instance.textMesh.text == transformedMessage)
                    {
                        Singleton<TextDisplayer>.Instance.Clear();
                    }
                }
            }
            Game_Patches.OpponentReady = false;

            if (InscryptionNetworking.Connection.IsHost && !InscryptionNetworking.START_ALONE)
            {
                if (InscryptionNetworking.Connection.PlayAgainstBot || new Random().Next(0, 2) != 0)
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
        public override IEnumerator GameEnd(bool playerWon)
        {
            foreach (CardSlot slot in Player_Backline.PlayerQueueSlots)
            {
                Destroy(slot.gameObject);
            }
            yield break;
        }
    }

    public class Multiplayer_Final_Battle_Sequencer : Multiplayer_Battle_Sequencer
    {
        public override IEnumerator GameEnd(bool playerWon)
        {
            Singleton<InteractionCursor>.Instance.InteractionDisabled = true;
            yield return new WaitForSeconds(1.5f);
            Singleton<UIManager>.Instance.Effects.GetEffect<ScreenColorEffect>().SetColor(GameColors.Instance.nearBlack);
            Singleton<UIManager>.Instance.Effects.GetEffect<ScreenColorEffect>().SetIntensity(1f, float.MaxValue);
            AudioController.Instance.StopAllLoops();
            Singleton<InteractionCursor>.Instance.SetHidden(hidden: true);
            yield return new WaitForSeconds(3f);
            AscensionMenuScreens.ReturningFromSuccessfulRun = playerWon;
            AscensionMenuScreens.ReturningFromFailedRun = !playerWon;
            SceneLoader.Load("Ascension_Configure");
        }
    }
}
