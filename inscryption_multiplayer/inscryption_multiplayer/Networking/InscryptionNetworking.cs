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
                case "start_game":
                    Game_Patches.OpponentReady = false;
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

            //messages here should be received by all users in the lobby except by the one who sent it
            if (!selfMessage)
            {
                switch (message)
                {
                    case "OpponentReady":
                        Game_Patches.OpponentReady = true;
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
                        internalCardInfo.Mods = cardInfo.mods ?? new List<CardModificationInfo>();

                        PlayableCard playableCard = CardSpawner.SpawnPlayableCard(internalCardInfo);
                        playableCard.TemporaryMods = cardInfo.temporaryMods ?? new List<CardModificationInfo>();

                        if (!cardInfo.slot.isPlayerSlot)
                        {
                            playableCard.SetIsOpponentCard(true);
                            Singleton<TurnManager>.Instance.Opponent.ModifySpawnedCard(playableCard);
                        }

                        Singleton<BoardManager>.Instance.StartCoroutine(CallbackRoutine(
                            Singleton<BoardManager>.Instance.TransitionAndResolveCreatedCard(
                                playableCard,
                                placedSlot,
                                0.1f,
                                false
                            ), () =>
                            {
                                if (PlayAgainstBot)
                                    Send("bypasscheck OpponentCardPlacePhaseEnded");
                            }));
                        break;

                    case "CardPlacedByOpponentInQueue":
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

                        Singleton<BoardManager>.Instance.QueueCardForSlot(playableCardQueue, slotToQueueFor, 0.25f);
                        Singleton<Opponent>.Instance.Queue.Add(playableCardQueue);

                        if (PlayAgainstBot)
                        {
                            Send("bypasscheck OpponentCardPlacePhaseEnded");
                        }
                        break;

                    case "CardSacrificedByOpponent":
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

        internal virtual void StartGame(bool resetBot = true)
        {
            if (resetBot)
                PlayAgainstBot = false;
            Send("start_game");
        }

        internal virtual void StartGameWithBot()
        {
            PlayAgainstBot = true;
            StartGame(false);
        }

        internal void SendSettings()
        {
            SendJson("Settings", GameSettings.Current);
        }

        private static IEnumerator CallbackRoutine(IEnumerator coroutine, Action callback)
        {
            yield return coroutine;
            callback();
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