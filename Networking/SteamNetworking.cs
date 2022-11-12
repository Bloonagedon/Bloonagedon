using Steamworks;
using System;
using System.Text;
using System.Text.Json;
using UnityEngine;

namespace inscryption_multiplayer.Networking
{
    internal class SteamNetworking : InscryptionNetworking
    {
        public const bool START_ALONE = false;

        public static byte[] SpaceByte;

        public static CSteamID? LobbyID;
        private CSteamID? OtherPlayerID;
        private string OtherPlayerName;
        private bool Connecting;

        private CallResult<LobbyCreated_t> lobbyCreated;
        private CallResult<LobbyEnter_t> lobbyEnter;

        private Callback<LobbyChatUpdate_t> lobbyChatUpdate;
        private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        private Callback<LobbyChatMsg_t> lobbyChatMsg;

        internal override bool Connected => LobbyID != null;
        internal override bool IsHost => SteamMatchmaking.GetLobbyOwner((CSteamID)LobbyID) != OtherPlayerID;

        internal override void Host()
        {
            lobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2));
        }

        internal override void SendJson(string message, object serializedClass)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            //converts it to bytes immediately as that's 5-10% faster than converting a serialized class to a string
            byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(serializedClass);

            //combines the three byte arrays
            byte[] fullMessageBytes = new byte[messageBytes.Length + SpaceByte.Length + jsonUtf8Bytes.Length];
            Buffer.BlockCopy(messageBytes, 0, fullMessageBytes, 0, messageBytes.Length);
            Buffer.BlockCopy(SpaceByte, 0, fullMessageBytes, messageBytes.Length, SpaceByte.Length);
            Buffer.BlockCopy(jsonUtf8Bytes, 0, fullMessageBytes, messageBytes.Length + SpaceByte.Length, jsonUtf8Bytes.Length);

            Send(fullMessageBytes);
        }

        internal override void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
        }

        internal override void Send(byte[] message)
        {
            SteamMatchmaking.SendLobbyChatMsg((CSteamID)LobbyID, message, message.Length);
        }

        private void OnLobbyCreated(LobbyCreated_t callback, bool fail)
        {
            Connecting = false;
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log("Lobby created successfully");
                LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
                if (START_ALONE)
                    Send("start_game");
            }
            else
            {
                Debug.Log($"Failed to create lobby ({callback.m_eResult})");
            }
        }

        private void OnLobbyEnter(LobbyEnter_t callback, bool fail)
        {
            Connecting = false;
            if (callback.m_EChatRoomEnterResponse == 1)
            {
                Debug.Log("Lobby joined successfully");
                LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
                OtherPlayerID = SteamMatchmaking.GetLobbyOwner((CSteamID)LobbyID);
            }
            else
            {
                Debug.Log($"Failed to join lobby ({callback.m_EChatRoomEnterResponse})");
            }
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            if ((callback.m_rgfChatMemberStateChange & 0x0001) == 0x0001)
            {
                OtherPlayerID = new CSteamID(callback.m_ulSteamIDMakingChange);
                Send("start_game");
            }
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            Plugin.Log.LogInfo("Lobby join request!");
            Connecting = true;
            lobbyEnter.Set(SteamMatchmaking.JoinLobby(callback.m_steamIDLobby));
        }

        private void OnLobbyChatMsg(LobbyChatMsg_t callback)
        {
            var selfMessage = callback.m_ulSteamIDUser != OtherPlayerID?.m_SteamID;
            if ((START_ALONE || !selfMessage) && callback.m_eChatEntryType == 1)
            {
                var buffer = new byte[4096];
                var numBytes = SteamMatchmaking.GetLobbyChatEntry((CSteamID)LobbyID, (int)callback.m_iChatID, out _,
                    buffer, buffer.Length, out _);
                if (numBytes == 0)
                    return;
                var message = new byte[numBytes];
                Array.Copy(buffer, message, numBytes);
                Receive(selfMessage, message);
            }
        }

        internal SteamNetworking()
        {
            lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);

            lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyEnter = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
        }
    }
}