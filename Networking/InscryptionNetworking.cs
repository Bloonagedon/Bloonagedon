using DiskCardGame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UnityEngine;

namespace inscryption_multiplayer.Networking
{
    internal abstract class InscryptionNetworking
    {
        internal static InscryptionNetworking Connection = new SteamNetworking();

        internal abstract bool Connected { get; }
        internal abstract bool IsHost { get; }
        internal abstract void Host();

        internal abstract void SendJson(string message, object serializedClass);
        internal abstract void Send(string message);
        internal abstract void Send(byte[] message);

        protected void Receive(bool selfMessage, string message)
        {
            string jsonString = null;
            List<string> wordList = message.Split(' ').ToList();
            if (wordList.Count > 1)
            {
                if (wordList[1][0] == '{')
                {
                    jsonString = string.Join(" ", message.Split(' ').Skip(1));
                    message = message.Split(' ')[0];
                }
            }

            Debug.Log($"Received message: {message}");
            if (jsonString != null)
            {
                Plugin.Log.LogInfo($"Json string: {jsonString}");
            }

            //messages here should be received by all users in the lobby
            switch (message)
            {
                case "start_game":

                    //starts a new run
                    Singleton<AscensionMenuScreens>.Instance.TransitionToGame(true);
                    Plugin.Log.LogInfo("started a game!");
                    break;

                    //everything below here is for testing, it shouldn't be here for release or when testing it with another player
            }

            //messages here should be received by all users in the lobby except by the one who sended it
            if (!selfMessage)
            {
                switch (message)
                {
                    case "OpponentReady":
                        ((Multiplayer_Battle_Sequencer)Singleton<TurnManager>.Instance.SpecialSequencer).OpponentReady = true;
                        break;

                    case "InitiateCombat":
                        Singleton<TurnManager>.Instance.playerInitiatedCombat = true;
                        break;

                    case "OpponentCardPlacePhaseEnded":
                        ((Multiplayer_Battle_Sequencer)Singleton<TurnManager>.Instance.SpecialSequencer).OpponentCardPlacePhase = false;
                        break;

                    case "CardPlacedByOpponent":
                        CardInfoMultiplayer cardInfo = JsonSerializer.Deserialize<CardInfoMultiplayer>(jsonString);
                        CardSlot placedSlot = Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == cardInfo.slot.index && x.IsPlayerSlot == cardInfo.slot.isPlayerSlot);
                        if (placedSlot.Card != null)
                        {
                            placedSlot.Card.ExitBoard(0, new Vector3(0, 0, 0));
                        }

                        Singleton<BoardManager>.Instance.StartCoroutine(Singleton<BoardManager>.Instance.CreateCardInSlot(
                            CardLoader.GetCardByName(cardInfo.name),
                            placedSlot,
                            0.1f,
                            false
                            ));
                        break;

                    case "CardSacrificedByOpponent":
                        CardSlotMultiplayer cardSlot = JsonSerializer.Deserialize<CardSlotMultiplayer>(jsonString);
                        CardSlot sacrificedSlot = Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == cardSlot.index && x.IsPlayerSlot == cardSlot.isPlayerSlot);

                        if (sacrificedSlot.Card != null)
                        {
                            sacrificedSlot.Card.ExitBoard(0, new Vector3(0, 0, 0));
                        }
                        break;
                }
            }
        }

        protected void Receive(bool selfMessage, byte[] message)
        {
            Receive(selfMessage, Encoding.UTF8.GetString(message).TrimEnd('\0'));
        }
    }
}