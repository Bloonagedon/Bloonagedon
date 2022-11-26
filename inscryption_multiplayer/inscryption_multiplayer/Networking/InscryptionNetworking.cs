using DiskCardGame;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace inscryption_multiplayer.Networking
{
    internal abstract class InscryptionNetworking : IDisposable
    {
        public const bool START_ALONE = false;
        private static readonly byte[] SpaceByte;
        
        internal static InscryptionNetworking Connection =
#if NETWORKING_STEAM
            new SteamNetworking();
#else
            new SocketNetworking();
#endif

        internal bool AutoStart;
        internal string OtherPlayerName;
        
        internal abstract bool Connected { get; }
        internal abstract bool IsHost { get; }
        internal abstract void Host();
        internal abstract void Join();
        internal virtual void Invite() {}
        internal abstract void Leave();
        internal abstract void Send(byte[] message);
        internal virtual void UpdateSettings() {}
        internal abstract void Update();
        public virtual void Dispose() {}

        internal void SendJson(string message, object serializedClass)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] jsonUtf8Bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serializedClass));

            //combines the three byte arrays
            byte[] fullMessageBytes = new byte[messageBytes.Length + SpaceByte.Length + jsonUtf8Bytes.Length];
            Buffer.BlockCopy(messageBytes, 0, fullMessageBytes, 0, messageBytes.Length);
            Buffer.BlockCopy(SpaceByte, 0, fullMessageBytes, messageBytes.Length, SpaceByte.Length);
            Buffer.BlockCopy(jsonUtf8Bytes, 0, fullMessageBytes, messageBytes.Length + SpaceByte.Length, jsonUtf8Bytes.Length);

            Send(fullMessageBytes);
        }

        internal void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        internal void Receive(bool selfMessage, string message)
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
                    //Singleton<AscensionMenuScreens>.Instance.TransitionToGame(true);
                    AscensionSaveData.Data.NewRun(StarterDecksUtil.GetInfo(AscensionSaveData.Data.currentStarterDeck).cards);
                    SaveManager.SaveToFile(false);
                    MenuController.LoadGameFromMenu(false);
                    Singleton<InteractionCursor>.Instance.SetHidden(true);
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
                        CardInfoMultiplayer cardInfo = JsonConvert.DeserializeObject<CardInfoMultiplayer>(jsonString);
                        CardSlot placedSlot = Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == cardInfo.slot.index && x.IsPlayerSlot == cardInfo.slot.isPlayerSlot);
                        if (placedSlot.Card != null)
                        {
                            placedSlot.Card.ExitBoard(0, new Vector3(0, 0, 0));
                        }

                        var internalCardInfo = CardLoader.GetCardByName(cardInfo.name);
                        internalCardInfo.Mods = cardInfo.mods;
                        Singleton<BoardManager>.Instance.StartCoroutine(Singleton<BoardManager>.Instance.CreateCardInSlot(
                            internalCardInfo,
                            placedSlot,
                            0.1f,
                            false
                            ));
                        break;

                    case "CardSacrificedByOpponent":
                        CardSlotMultiplayer cardSlot = JsonConvert.DeserializeObject<CardSlotMultiplayer>(jsonString);
                        CardSlot sacrificedSlot = Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == cardSlot.index && x.IsPlayerSlot == cardSlot.isPlayerSlot);

                        if (sacrificedSlot.Card != null)
                        {
                            sacrificedSlot.Card.ExitBoard(0, new Vector3(0, 0, 0));
                        }
                        break;
                    
                    case "Settings":
                        GameSettings.Current = JsonConvert.DeserializeObject<GameSettings>(jsonString);
                        break;
                }
            }
        }

        internal void Receive(bool selfMessage, byte[] message)
        {
            Receive(selfMessage, Encoding.UTF8.GetString(message).TrimEnd('\0'));
        }

        internal virtual void StartGame()
        {
            Send("start_game");
        }

        internal void SendSettings()
        {
            SendJson("Settings", GameSettings.Current);
        }

        ~InscryptionNetworking()
        {
            Dispose();
        }

        static InscryptionNetworking()
        {
            SpaceByte = Encoding.UTF8.GetBytes(" ");
        }
    }
}