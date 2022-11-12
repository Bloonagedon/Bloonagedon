using DiskCardGame;
using GBC;
using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using inscryption_multiplayer.Networking;
using UnityEngine;

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

            // Clone the new button
            AscensionMenuInteractable lobbyButtonController = CreateAscensionButton(menuText);

            // Add to transition

            onEnableRevealedObjects.Insert(onEnableRevealedObjects.IndexOf(menuText.gameObject) + 1, lobbyButtonController.gameObject);
            screenInteractables.Insert(screenInteractables.IndexOf(menuText) + 1, lobbyButtonController);
            lobbyButtonController.CursorSelectStarted = delegate (MainInputInteractable interactable)
            {
                InscryptionNetworking.Connection.Host();
                Plugin.Log.LogInfo("Started a lobby!");
            };
            itemsSpacing.menuItems.Insert(1, lobbyButtonController.transform);

            for (int i = 1; i < itemsSpacing.menuItems.Count; i++)
            {
                Transform item = itemsSpacing.menuItems[i];
                item.localPosition = new Vector2(item.localPosition.x, i * -0.11f);
            }
        }

        public static AscensionMenuInteractable CreateAscensionButton(AscensionMenuInteractable newRunButton)
        {
            AscensionMenuInteractable newLobbyButton = GameObject.Instantiate(newRunButton, newRunButton.transform.parent);
            newLobbyButton.name = "Menu_New_Lobby";
            newLobbyButton.CursorSelectStarted = delegate
            {
                newRunButton.CursorSelectStart();
            };
            newLobbyButton.GetComponentInChildren<PixelText>().SetText("- CREATE LOBBY -");

            return newLobbyButton;
        }
    }
}
