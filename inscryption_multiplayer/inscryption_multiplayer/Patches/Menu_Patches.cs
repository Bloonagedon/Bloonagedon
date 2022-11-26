using System.Collections;
using DiskCardGame;
using GBC;
using HarmonyLib;
using inscryption_multiplayer.Networking;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Menu_Patches
    {
        internal static bool JoiningLobby;
        private static MenuController MenuControllerInstance;
        
        [HarmonyPatch(typeof(StartScreenController), nameof(StartScreenController.Start))]
        [HarmonyPostfix]
        private static void CreateMultiplayerUI(MenuController ___menu)
        {
            MenuControllerInstance = ___menu;
            
            var ui = UnityEngine.Object.Instantiate(MultiplayerAssetHandler.MultiplayerSettingsUI, ___menu.transform);
            var menuUI = ui.GetComponent<InscryptionMultiplayerMenuUI>();
            SetupMultiplayerMenuUI(menuUI);
            ui.SetActive(false);
            MultiplayerAssetHandler.MultiplayerSettingsUIInstance = menuUI;
            
            var multiplayerMenuCard = UnityEngine.Object.Instantiate(MultiplayerAssetHandler.MultiplayerMenuCard,
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
            InscryptionNetworking.Connection.Leave();
            var ui = MultiplayerAssetHandler.MultiplayerSettingsUIInstance;
            ui.ResetMenu();
            ui.gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadFromFile))]
        [HarmonyPostfix]
        private static void SetAscension()
        {
            if(Plugin.MultiplayerActive)
                SaveFile.IsAscension = true;
        }
        
        [HarmonyPatch(typeof(MenuController), nameof(MenuController.TransitionToAscensionMenu))]
        [HarmonyPrefix]
        private static bool ReplaceAscension(MenuController __instance, ref IEnumerator __result)
        {
            if (Plugin.MultiplayerActive)
            {
                InscryptionNetworking.Connection.Leave();
                SaveFile.IsAscension = false;
                SaveManager.SaveToFile(false);
                __result = __instance.TransitionToStartMenu();
                return false;
            }
            return true;
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
                UpdateSettings();
                menu.SaveSettingsText.text = "Start";
                menu.TabGroup_Settings.SetActive(true);
            };
            
            menu.QuickplayCancelButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                InscryptionNetworking.Connection.Leave();
                menu.ResetMenu();
            };

            menu.LobbyAccessButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                GameSettings.Current.LobbyType = GameSettings.Current.LobbyType == LobbyAccess.InviteOnly
                    ? LobbyAccess.FriendsOnly
                    : LobbyAccess.InviteOnly;
                UpdateSettings();
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
                if (InscryptionNetworking.Connection.IsHost)
                {
                    InscryptionNetworking.Connection.Leave();
                    menu.ResetMenu();
                }
                else
                {
                    CancelMultiplayer();
                    MenuControllerInstance.SetCardsEnabled(true);
                }
            };
            menu.ChangeSettingsButton.GetInternalComponent<GenericUIButton>().OnButtonUp = _ =>
            {
                menu.TabGroup_Lobby.SetActive(false);
                menu.SaveSettingsText.text = "SAVE";
                menu.TabGroup_Settings.SetActive(true);
            };
        }

        private static void UpdateSettings()
        {
            var ui = MultiplayerAssetHandler.MultiplayerSettingsUIInstance;
            ui.LobbyAccessText.text = GameSettings.Current.LobbyType == LobbyAccess.InviteOnly
                ? "Invite only"
                : "Friends can join";
        }
    }
}