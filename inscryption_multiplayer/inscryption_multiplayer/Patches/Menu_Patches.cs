using System.Collections;
using DiskCardGame;
using GBC;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using UnityEngine;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Menu_Patches
    {
        internal static bool JoiningLobby;
        internal static NetworkingError MultiplayerError;
        internal static MenuController MenuControllerInstance;

        [HarmonyPatch(typeof(StartScreenController), nameof(StartScreenController.Start))]
        [HarmonyPostfix]
        private static void CreateMultiplayerUI(MenuController ___menu)
        {
            MenuControllerInstance = ___menu;
            
            var ui = Object.Instantiate(MultiplayerAssetHandler.MultiplayerSettingsUI, ___menu.transform);
            var menuUI = ui.GetComponent<InscryptionMultiplayerMenuUI>();
            SetupMultiplayerMenuUI(menuUI);
            ui.SetActive(false);
            MultiplayerAssetHandler.MultiplayerSettingsUIInstance = menuUI;
            
            var errorUIObject = Object.Instantiate(MultiplayerAssetHandler.MultiplayerErrorUI, ___menu.transform);
            var errorUI = errorUIObject.GetComponent<ErrorUI>();
            errorUI.ViewStatsButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                AscensionMenuScreens.ReturningFromSuccessfulRun = !MultiplayerError.OwnFault;
                AscensionMenuScreens.ReturningFromFailedRun = MultiplayerError.OwnFault;
                Time.timeScale = 1;
                SceneLoader.Load("Ascension_Configure");
            };
            errorUI.CloseButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                MultiplayerError = null;
                errorUIObject.SetActive(false);
                ___menu.SetCardsEnabled(true);
            };
            if (MultiplayerError != null)
            {
                ___menu.SetCardsEnabled(false);
                errorUI.ErrorDescription.text = MultiplayerError.Message;
                errorUIObject.SetActive(true);
            }
            else errorUIObject.SetActive(false);
            MultiplayerAssetHandler.MultiplayerErrorUIInstance = errorUIObject;
            
            var multiplayerMenuCard = Object.Instantiate(MultiplayerAssetHandler.MultiplayerMenuCard,
                ___menu.cards[0].transform.parent);
            multiplayerMenuCard.SetActive(true);
            multiplayerMenuCard.GetComponent<MenuCardProxy>().ProxyComponent();
            var menuCard = multiplayerMenuCard.GetComponent<MenuCard>();
            var cardInitializer = ___menu.GetComponentInChildren<StartMenuAscensionCardInitializer>(true);
            ___menu.cards.Insert(2, menuCard);
            cardInitializer.menuCards.Insert(2, menuCard);
            cardInitializer.ReconfigureCardRow();
            MultiplayerAssetHandler.MultiplayerMenuCardInstance = multiplayerMenuCard;

            if (JoiningLobby)
            {
                JoiningLobby = false;
                ___menu.SetCardsEnabled(false);
                menuUI.TabGroup_JoinMode.SetActive(false);
                menuUI.TabGroup_Lobby.SetActive(true);
                menuUI.OtherPlayerText.text = $"OTHER PLAYER: {InscryptionNetworking.Connection.OtherPlayerName}";
                menuUI.ChangeSettingsButton.gameObject.SetActive(false);
                menuUI.StartGameButton.gameObject.SetActive(false);
                menuUI.gameObject.SetActive(true);
            }
        }

        [HarmonyPatch(typeof(MenuController), nameof(MenuController.OnCardReachedSlot))]
        [HarmonyPostfix]
        private static void OnModCardReachedSlot(MenuController __instance, MenuCard card)
        {
            switch ((int)card.menuAction)
            {
                case 101:
                    __instance.Shake(0.01f, 0.25f);
                    __instance.StartCoroutine(TransitionToMultiplayer(__instance, card));
                    break;
            }
        }
        
        public static void LeaveGuest()
        {
            CancelMultiplayer();
            MenuControllerInstance.SetCardsEnabled(true);
        }

        private static IEnumerator TransitionToMultiplayer(MenuController controller, MenuCard card)
        {
            controller.DoingCardTransition = true;
            yield return controller.TransitionToSlottedState(card, -1.56f, false);
            controller.DoingCardTransition = false;
            MultiplayerAssetHandler.MultiplayerSettingsUIInstance.gameObject.SetActive(true);
            controller.DisplayMenuCardTitle(null);
        }

        [HarmonyPatch(typeof(MenuController), nameof(MenuController.EndSpecialStates))]
        [HarmonyPostfix]
        private static void CancelMultiplayer()
        {
            if(SceneLoader.ActiveSceneName != SceneLoader.StartSceneName)
                return;
            InscryptionNetworking.Connection.Leave();
            var ui = MultiplayerAssetHandler.MultiplayerSettingsUIInstance;
            ui.ResetMenu();
            ui.gameObject.SetActive(false);
        }

        private static void SetupMultiplayerMenuUI(InscryptionMultiplayerMenuUI menu)
        {
            menu.QuickplayText.text =
#if NETWORKING_STEAM
                "Quickplay";
#else
                "Join";
#endif
            menu.JoinButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                menu.TabGroup_JoinMode.SetActive(false);
                menu.TabGroup_Quickplay.SetActive(true);
                GameSettings.Current = new GameSettings();
                InscryptionNetworking.Connection.Join();
            };
            menu.HostButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                menu.TabGroup_JoinMode.SetActive(false);
                GameSettings.Current = new GameSettings();
                UpdateSettingsVisuals();
                menu.SaveSettingsText.text = "Start";
                menu.TabGroup_Settings.SetActive(true);
            };
            
            menu.QuickplayCancelButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                InscryptionNetworking.Connection.Leave();
                menu.ResetMenu();
            };

            menu.StartGameButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                if (InscryptionNetworking.Connection.IsHost)
                {
                    if(menu.StartGameText.text == "INVITE PLAYER")
                        InscryptionNetworking.Connection.Invite();
                    else
                    {
                        menu.gameObject.SetActive(false);
                        InscryptionNetworking.Connection.StartGame();
                    }
                }
            };
            menu.LeaveLobbyButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                if (!InscryptionNetworking.Connection.TransferredHost && InscryptionNetworking.Connection.IsHost)
                {
                    InscryptionNetworking.Connection.Leave();
                    menu.ResetMenu();
                }
                else LeaveGuest();
            };
            menu.ChangeSettingsButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                menu.TabGroup_Lobby.SetActive(false);
                menu.SaveSettingsText.text = "SAVE";
                menu.TabGroup_Settings.SetActive(true);
            };
            menu.PlayWithBotButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                InscryptionNetworking.Connection.StartGameWithBot();
            };
            
            menu.LobbyAccessButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                GameSettings.Current.LobbyType = GameSettings.Current.LobbyType == LobbyAccess.InviteOnly
                    ? LobbyAccess.FriendsOnly
                    : LobbyAccess.InviteOnly;
                UpdateSettingsVisuals();
            };
            menu.SettingsStartButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                menu.TabGroup_Settings.SetActive(false);
                if (InscryptionNetworking.Connection.Connected)
                {
                    InscryptionNetworking.Connection.UpdateSettings();
                    InscryptionNetworking.Connection.SendSettings();
                }
                else
                {
                    InscryptionNetworking.Connection.Host();
                    menu.OtherPlayerText.text = "OTHER PLAYER: ";
                    menu.StartGameText.text = "INVITE PLAYER";
                }
                menu.ChangeSettingsButton.gameObject.SetActive(true);
                menu.StartGameButton.gameObject.SetActive(true);
                menu.TabGroup_Lobby.SetActive(true);
            };
            menu.MapsPlusButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                GameSettings.Current.MapsUsed = Mathf.Min(GameSettings.Current.MapsUsed + 1, 99999);
                UpdateSettingsVisuals();
            };
            menu.MapsMinusButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                GameSettings.Current.MapsUsed = Mathf.Max(GameSettings.Current.MapsUsed - 1, 1);
                UpdateSettingsVisuals();
            };
            menu.ToggleTotemsButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                GameSettings.Current.AllowTotems ^= true;
                UpdateSettingsVisuals();
            };
        }

        private static void UpdateSettingsVisuals()
        {
            var ui = MultiplayerAssetHandler.MultiplayerSettingsUIInstance;
            ui.LobbyAccessText.text = GameSettings.Current.LobbyType == LobbyAccess.InviteOnly
                ? "INVITE ONLY"
                : "FRIENDS CAN JOIN";
            ui.MapsNumberText.text = $"{GameSettings.Current.MapsUsed} + 1";
            ui.TotemSettingText.text = GameSettings.Current.AllowTotems ? "TOTEMS ENABLED" : "TOTEMS DISABLED";
        }
    }
}
