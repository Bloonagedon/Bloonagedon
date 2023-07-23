using DiskCardGame;
using inscryption_multiplayer.Patches;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace inscryption_multiplayer.Networking
{
    public static class NetworkingMessage
    {
        public const string StartGame = "start_game";
        public const string OpponentReady = "OpponentReady";
        public const string InitiateCombat = "InitiateCombat";
        public const string OpponentCardPlacePhaseEnded = "OpponentCardPlacePhaseEnded";
        public const string CardPlacedByOpponent = "CardPlacedByOpponent";
        public const string CardQueuedByOpponent = "CardPlacedByOpponentInQueue";
        public const string CardSacrificedByOpponent = "CardSacrificedByOpponent";
        public const string EggPlaced = "EggPlaced";
        public const string ChangeOpponentTotem = "ChangeOpponentTotem";
        public const string ItemUsed = "ItemUsed";
        public const string ChangeSettings = "ChangeSettings";
    }
    
    public class NetworkingError
    {
        public static NetworkingError OtherPlayerLeft = new("Other player left", false);
        public static NetworkingError ConnectionLost = new("Connection lost", true);
        public static NetworkingError GaveUp = new("Gave up", true);
        
        public readonly string Message;
        public readonly bool OwnFault;
        
        public NetworkingError(string message, bool ownFault)
        {
            Message = message;
            OwnFault = ownFault;
        }
    }
    
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
        internal bool TransferredHost;

        internal bool PlayAgainstBot;

        internal abstract bool Connected { get; }
        internal abstract bool IsHost { get; }
        internal abstract void Host();
        internal abstract void Join();
        internal virtual void Invite() { }
        internal abstract void Leave();
        internal abstract void Send(byte[] message);
        internal virtual void UpdateSettings() { }
        internal abstract void Update();
        public virtual void Dispose() { }

        internal void SendJson(string message, object serializedClass, bool bypassCheck = false)
        {
            if (bypassCheck)
                message = "bypasscheck " + message;
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] jsonUtf8Bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serializedClass));

            //combines the three byte arrays
            byte[] fullMessageBytes = new byte[messageBytes.Length + SpaceByte.Length + jsonUtf8Bytes.Length];
            Buffer.BlockCopy(messageBytes, 0, fullMessageBytes, 0, messageBytes.Length);
            Buffer.BlockCopy(SpaceByte, 0, fullMessageBytes, messageBytes.Length, SpaceByte.Length);
            Buffer.BlockCopy(jsonUtf8Bytes, 0, fullMessageBytes, messageBytes.Length + SpaceByte.Length, jsonUtf8Bytes.Length);

            Send(fullMessageBytes);
        }

        internal void Send(string message, bool bypassCheck = false)
        {
            if (bypassCheck)
                message = "bypasscheck " + message;
            Send(Encoding.UTF8.GetBytes(message));
        }

        internal void Receive(bool selfMessage, string message)
        {
            if (message.StartsWith("bypasscheck "))
            {
                message = message.Substring(12);
                if (PlayAgainstBot)
                    selfMessage = false;
            }
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
                case NetworkingMessage.StartGame:
                    MultiplayerRunState.Run = new();
                    Game_Patches.OpponentReady = false;
                    //starts a new run
                    //Singleton<AscensionMenuScreens>.Instance.TransitionToGame(true);

                    Ascension_Patches.DeckSelection = true;
                    Time.timeScale = 1;
                    SceneLoader.Load("Ascension_Configure");
                    
                    break;

                    //everything below here is for testing, it shouldn't be here for release or when testing it with another player
            }

            //messages here should be received by all users in the lobby except by the one who sent it
            if (!selfMessage)
            {
                switch (message)
                {
                    case NetworkingMessage.OpponentReady:
                        Game_Patches.OpponentReady = true;
                        break;

                    case NetworkingMessage.InitiateCombat:
                        Singleton<TurnManager>.Instance.playerInitiatedCombat = true;
                        break;

                    case NetworkingMessage.OpponentCardPlacePhaseEnded:
                        Multiplayer_Battle_Sequencer.Current.OpponentEvents.Enqueue(ToggleCardPlacePhase());
                        //Multiplayer_Battle_Sequencer.Current.OpponentCardPlacePhase = false;
                        break;

                    case NetworkingMessage.CardPlacedByOpponent:
                        CardInfoMultiplayer cardInfo = JsonConvert.DeserializeObject<CardInfoMultiplayer>(jsonString);
                        CardSlot placedSlot = Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == cardInfo.slot.index && x.IsPlayerSlot == cardInfo.slot.isPlayerSlot);
                        if (placedSlot.Card != null)
                        {
                            placedSlot.Card.ExitBoard(0, new Vector3(0, 0, 0));
                        }

                        var internalCardInfo = CardLoader.GetCardByName(cardInfo.name);
                        internalCardInfo.Mods = cardInfo.mods ?? new List<CardModificationInfo>();

                        PlayableCard playableCard = CardSpawner.SpawnPlayableCard(internalCardInfo);
                        playableCard.TemporaryMods = cardInfo.temporaryMods ?? new List<CardModificationInfo>();
                        
                        playableCard.OnStatsChanged();

                        if (!cardInfo.slot.isPlayerSlot)
                        {
                            playableCard.SetIsOpponentCard(true);
                            Singleton<TurnManager>.Instance.Opponent.ModifySpawnedCard(playableCard);
                        }

                        Singleton<BoardManager>.Instance.StartCoroutine(Utils.CallbackRoutine(
                            Singleton<BoardManager>.Instance.TransitionAndResolveCreatedCard(
                                playableCard,
                                placedSlot,
                                0.1f,
                                false
                            ), () =>
                            {
                                if (PlayAgainstBot)
                                    Send(NetworkingMessage.OpponentCardPlacePhaseEnded, true);
                            }));
                        break;

                    case NetworkingMessage.CardQueuedByOpponent:
                        CardInfoMultiplayer cardInfoQueue = JsonConvert.DeserializeObject<CardInfoMultiplayer>(jsonString);
                        CardSlot slotToQueueFor = Singleton<BoardManager>.Instance.opponentSlots.FirstOrDefault(x => x.Index == cardInfoQueue.slot.index);

                        if (slotToQueueFor == null)
                        {
                            break;
                        }

                        PlayableCard cardInQueue = Singleton<TurnManager>.Instance.Opponent.Queue.FirstOrDefault(x => x.Slot.Index == cardInfoQueue.slot.index);

                        if (cardInQueue != null)
                        {
                            cardInQueue.ExitBoard(0, new Vector3(0, 0, 0));
                        }

                        var internalCardInfoQueue = CardLoader.GetCardByName(cardInfoQueue.name);
                        internalCardInfoQueue.Mods = cardInfoQueue.mods ?? new List<CardModificationInfo>();

                        Singleton<ViewManager>.Instance.SwitchToView(Singleton<BoardManager>.Instance.QueueView, false, false);

                        PlayableCard playableCardQueue = Singleton<Opponent>.Instance.CreateCard(internalCardInfoQueue);
                        playableCardQueue.TemporaryMods = cardInfoQueue.temporaryMods ?? new List<CardModificationInfo>();

                        playableCardQueue.OnStatsChanged();

                        Singleton<BoardManager>.Instance.QueueCardForSlot(playableCardQueue, slotToQueueFor, 0.25f);
                        Singleton<Opponent>.Instance.Queue.Add(playableCardQueue);

                        if (PlayAgainstBot)
                        {
                            Send(NetworkingMessage.OpponentCardPlacePhaseEnded, true);
                        }
                        break;

                    case NetworkingMessage.CardSacrificedByOpponent:
                        CardSlotMultiplayer cardSlot = JsonConvert.DeserializeObject<CardSlotMultiplayer>(jsonString);
                        PlayableCard sacrificedCard;
                        if (cardSlot.isQueueSlot)
                        {
                            sacrificedCard = Singleton<TurnManager>.Instance.Opponent.Queue.FirstOrDefault(x => x.Slot.Index == cardSlot.index);
                        }
                        else
                        {
                            CardSlot sacrificedSlot = Singleton<BoardManager>.Instance.AllSlots.First(x => x.Index == cardSlot.index && x.IsPlayerSlot == cardSlot.isPlayerSlot);
                            if (sacrificedSlot.Card == null)
                            {
                                break;
                            }
                            sacrificedCard = sacrificedSlot.Card;
                        }
                        if (sacrificedCard != null)
                        {
                            sacrificedCard.ExitBoard(0, new Vector3(0, 0, 0));
                        }
                        break;
                    
                    case NetworkingMessage.EggPlaced:
                        MultiplayerRunState.Run.EggQueue.Enqueue(wordList[1] == "1");
                        break;
                    
                    case NetworkingMessage.ChangeOpponentTotem:
                        MultiplayerRunState.Run.OpponentTotem =
                            JsonConvert.DeserializeObject<TotemDefinition>(jsonString);
                        break;
                    
                    case NetworkingMessage.ItemUsed:
                        Multiplayer_Battle_Sequencer.Current.OpponentEvents.Enqueue(
                            Item_Sync.HandleOpponentItem(JsonConvert.DeserializeObject<MultiplayerItemData>(jsonString)));
                        break;

                    case NetworkingMessage.ChangeSettings:
                        GameSettings.Current = JsonConvert.DeserializeObject<GameSettings>(jsonString);
                        break;
                }
            }
        }

        private IEnumerator ToggleCardPlacePhase()
        {
            Multiplayer_Battle_Sequencer.Current.OpponentCardPlacePhase = false;
            yield return null;
        }

        internal void Receive(bool selfMessage, byte[] message)
        {
            Receive(selfMessage, Encoding.UTF8.GetString(message).TrimEnd('\0'));
        }

        internal virtual void StartGame(bool resetBot = true)
        {
            if (resetBot)
                PlayAgainstBot = false;
            Send(NetworkingMessage.StartGame);
        }

        internal virtual void StartGameWithBot()
        {
            PlayAgainstBot = true;
            StartGame(false);
        }

        internal void SendSettings()
        {
            SendJson(NetworkingMessage.ChangeSettings, GameSettings.Current);
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
