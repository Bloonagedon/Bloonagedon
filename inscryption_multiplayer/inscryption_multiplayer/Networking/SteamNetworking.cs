#if NETWORKING_STEAM

#if LINUX
extern alias SeparateSteamworks;
using SeparateSteamworks::Steamworks;
#else
using Steamworks;
#endif
using System;
using DiskCardGame;
using UnityEngine;

namespace inscryption_multiplayer.Networking
{
    internal class SteamNetworking : InscryptionNetworking
    {
        private static CSteamID? LobbyID;
        private readonly CSteamID PlayerID;
        private CSteamID? OtherPlayerID;
        private bool Connecting;
        private bool Quickplaying;

        private CallResult<LobbyMatchList_t> lobbyMatchList;
        private CallResult<LobbyCreated_t> lobbyCreated;
        private CallResult<LobbyEnter_t> lobbyEnter;

        private Callback<LobbyChatUpdate_t> lobbyChatUpdate;
        private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        private Callback<LobbyChatMsg_t> lobbyChatMsg;

        internal override bool Connected => LobbyID is not null;
        internal override bool IsHost => Connected && SteamMatchmaking.GetLobbyOwner((CSteamID)LobbyID) == PlayerID;

        internal override void Host()
        {
            AutoStart = false;
            lobbyCreated.Set(SteamMatchmaking.CreateLobby((ELobbyType)GameSettings.Current.LobbyType, 2));
        }

        internal override void Join()
        {
            var requestLobbyList = SteamMatchmaking.RequestLobbyList();
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);
            lobbyMatchList.Set(requestLobbyList);
        }

        internal override void Invite()
        {
            base.Invite();
            if(Connected)
                SteamFriends.ActivateGameOverlayInviteDialog((CSteamID)LobbyID);
        }

        internal override void Leave()
        {
            if (Connected)
            {
                SteamMatchmaking.LeaveLobby((CSteamID)LobbyID);
                LobbyID = null;
                OtherPlayerID = null;
                OtherPlayerName = null;
                Connecting = false;
            }
        }

        internal override void UpdateSettings()
        {
            if (IsHost)
                SteamMatchmaking.SetLobbyType((CSteamID)LobbyID, (ELobbyType)GameSettings.Current.LobbyType);
        }

        internal override void Update()
        {
            SteamAPI.RunCallbacks();
        }

        internal override void StartGame()
        {
            SteamMatchmaking.SetLobbyJoinable((CSteamID)LobbyID, false);
            base.StartGame();
        }

        internal override void Send(byte[] message)
        {
            SteamMatchmaking.SendLobbyChatMsg((CSteamID)LobbyID, message, message.Length);
        }

        private void OnLobbyMatchList(LobbyMatchList_t callback, bool fail)
        {
            if (!fail)
            {
                Debug.Log($"Querried lobbies: {callback.m_nLobbiesMatching}");
                if (callback.m_nLobbiesMatching == 0)
                {
                    Debug.Log("No lobbies found, creating one");
                    AutoStart = true;
                    lobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, 2));
                }
                else
                {
                    Debug.Log("Lobby found, joining");
                    Connecting = true;
                    Quickplaying = true;
                    lobbyEnter.Set(SteamMatchmaking.JoinLobby(SteamMatchmaking.GetLobbyByIndex(0)));
                }
            }
            else
            {
                Debug.Log("Failed to querry lobbies");
            }
        }

        private void OnLobbyCreated(LobbyCreated_t callback, bool fail)
        {
            Connecting = false;
            if (!fail && callback.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log("Lobby created successfully");
                LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
                SteamMatchmaking.SetLobbyJoinable((CSteamID)LobbyID, true);
                if (START_ALONE)
                    StartGame();
            }
            else
            {
                Debug.Log($"Failed to create lobby ({callback.m_eResult})");
            }
        }

        private void OnLobbyEnter(LobbyEnter_t callback, bool fail)
        {
            Connecting = false;
            if (!fail && callback.m_EChatRoomEnterResponse == 1)
            {
                Debug.Log("Lobby joined successfully");
                LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
                OtherPlayerID = SteamMatchmaking.GetLobbyOwner((CSteamID)LobbyID);
                OtherPlayerName = SteamFriends.GetFriendPersonaName((CSteamID)OtherPlayerID);
                if (!Quickplaying)
                {
                    Menu_Patches.JoiningLobby = true;
                    MenuController.ReturnToStartScreen();
                }
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
                OtherPlayerName = SteamFriends.GetFriendPersonaName((CSteamID)OtherPlayerID);
                if(AutoStart)
                    StartGame();
                else
                {
                    SendSettings();
                    var ui = MultiplayerAssetHandler.MultiplayerSettingsUIInstance;
                    ui.OtherPlayerText.text = $"OTHER PLAYER: {OtherPlayerName}";
                    ui.StartGameText.text = "START GAME";
                }
            }
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            Plugin.Log.LogInfo("Lobby join request!");
            Leave();
            Connecting = true;
            Quickplaying = false;
            lobbyEnter.Set(SteamMatchmaking.JoinLobby(callback.m_steamIDLobby));
        }

        private void OnLobbyChatMsg(LobbyChatMsg_t callback)
        {
            var selfMessage = callback.m_ulSteamIDUser != OtherPlayerID?.m_SteamID;
            if (callback.m_eChatEntryType == 1)
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

        public override void Dispose()
        {
            base.Dispose();
            Leave();
        }

        internal SteamNetworking()
        {
            PlayerID = SteamUser.GetSteamID();
            
            lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);

            lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyEnter = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
        }
    }
}

#endif