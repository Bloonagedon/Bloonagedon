using DiskCardGame;
using GBC;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using SteamNetworking = inscryption_multiplayer.Networking.SteamNetworking;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class KCM_multiplayer_option
    {
        [HarmonyPatch(typeof(AscensionStartScreen), nameof(AscensionStartScreen.Start))]
        [HarmonyPrefix]
        public static void Prefix(ref AscensionStartScreen __instance)
        {
            AdjustAscensionMenuItemsSpacing itemsSpacing = GameObject.FindObjectOfType<AdjustAscensionMenuItemsSpacing>();
            AscensionMenuInteractable menuText = itemsSpacing.menuItems[0].GetComponent<AscensionMenuInteractable>();

            AscensionMenuScreenTransition transitionController = AscensionMenuScreens.Instance.startScreen.GetComponent<AscensionMenuScreenTransition>();
            List<GameObject> onEnableRevealedObjects = transitionController.onEnableRevealedObjects;
            List<MainInputInteractable> screenInteractables = transitionController.screenInteractables;

            AscensionMenuInteractable inviteButtonController = CreateAscensionButton(menuText, "- INVITE A PLAYER -");
            AscensionMenuInteractable lobbyButtonController = CreateAscensionButton(menuText, "- CREATE LOBBY -");

            onEnableRevealedObjects.Insert(onEnableRevealedObjects.IndexOf(menuText.gameObject) + 1, lobbyButtonController.gameObject);
            screenInteractables.Insert(screenInteractables.IndexOf(menuText) + 1, lobbyButtonController);

            onEnableRevealedObjects.Insert(onEnableRevealedObjects.IndexOf(menuText.gameObject) + 2, inviteButtonController.gameObject);
            screenInteractables.Insert(screenInteractables.IndexOf(menuText) + 1, inviteButtonController);

            lobbyButtonController.CursorSelectStarted = delegate (MainInputInteractable interactable)
            {
                InscryptionNetworking.Connection.Host();
                Plugin.Log.LogInfo("Started a lobby!");
            };

            inviteButtonController.CursorSelectStarted = delegate (MainInputInteractable interactable)
            {
                if (InscryptionNetworking.Connection.Connected)
                {
                    SteamFriends.ActivateGameOverlayInviteDialog((CSteamID)SteamNetworking.LobbyID);
                }
            };

            itemsSpacing.menuItems.Insert(1, lobbyButtonController.transform);
            itemsSpacing.menuItems.Insert(2, inviteButtonController.transform);

            for (int i = 1; i < itemsSpacing.menuItems.Count; i++)
            {
                Transform item = itemsSpacing.menuItems[i];
                item.localPosition = new Vector2(item.localPosition.x, i * -0.11f);
            }
        }

        public static AscensionMenuInteractable CreateAscensionButton(AscensionMenuInteractable newRunButton, string text)
        {
            AscensionMenuInteractable newButton = GameObject.Instantiate(newRunButton, newRunButton.transform.parent);
            //newLobbyButton.name = "Menu_New_Lobby";
            newButton.CursorSelectStarted = delegate
            {
                newRunButton.CursorSelectStart();
            };
            newButton.GetComponentInChildren<PixelText>().SetText(text);

            return newButton;
        }
    }
}
