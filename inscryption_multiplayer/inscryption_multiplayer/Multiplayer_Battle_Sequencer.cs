using inscryption_multiplayer;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace DiskCardGame
{
    public class Multiplayer_Battle_Sequencer : SpecialBattleSequencer
    {
        public static Multiplayer_Battle_Sequencer Current =>
            (Multiplayer_Battle_Sequencer)Singleton<TurnManager>.Instance.SpecialSequencer;

        public bool OpponentCardPlacePhase = false;

        public Queue<IEnumerator> OpponentEvents = new();

        public override IEnumerator OpponentUpkeep()
        {
            if (GameSettings.Current.AllowBackrows && InscryptionNetworking.Connection.PlayAgainstBot)
            {
                foreach (PlayableCard card in Singleton<Opponent>.Instance.Queue)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    InscryptionNetworking.Connection.Send(NetworkingMessage.CardsInOpponentQueueMoved, true);
                }
            }

            OpponentCardPlacePhase = true;
            OpponentEvents.Clear();
            string transformedMessage = Singleton<TextDisplayer>.Instance.ShowMessage("waiting for opponent to finish their turn");
            if (!string.IsNullOrEmpty(transformedMessage))
            {
                Singleton<TextDisplayer>.Instance.CurrentAdvanceMode = TextDisplayer.MessageAdvanceMode.Auto;
                if (InscryptionNetworking.Connection.PlayAgainstBot)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    InscryptionNetworking.Connection.SendJson(GameSettings.Current.AllowBackrows ?
                                                              NetworkingMessage.CardQueuedByOpponent :
                                                              NetworkingMessage.CardPlacedByOpponent,
                                                              GlobalTriggerHandlerMultiplayer.TestCardInfo, true);
                }
                else
                {
                    while (OpponentCardPlacePhase)
                    {
                        while (OpponentEvents.Count > 0)
                            yield return OpponentEvents.Dequeue();
                        yield return null;
                    }
                }

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
            InscryptionNetworking.Connection.Send(NetworkingMessage.OpponentCardPlacePhaseEnded);
            yield break;
        }

        public override IEnumerator PlayerUpkeep()
        {
            if (GameSettings.Current.AllowBackrows)
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
            }
            yield break;
        }

        public static IEnumerator WaitForOpponent()
        {
            //i assign the trigger handler to a slot, there might be something better to attach it to but this seems the easiest for now
            GlobalTriggerHandlerMultiplayer triggerHandler = Singleton<BoardManager>.Instance.AllSlots[0].gameObject.AddComponent<GlobalTriggerHandlerMultiplayer>();
            Singleton<GlobalTriggerHandler>.Instance.RegisterNonCardReceiver(triggerHandler);

            InscryptionNetworking.Connection.Send(NetworkingMessage.OpponentReady, true);
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
                    InscryptionNetworking.Connection.Send(NetworkingMessage.InitiateCombat);
                }
            }
            yield break;
        }

        public override IEnumerator GameEnd(bool playerWon)
        {
            if (GameSettings.Current.AllowBackrows)
            {
                foreach (CardSlot slot in Player_Backline.PlayerQueueSlots)
                {
                    slot.gameObject.SetActive(false);
                }
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
