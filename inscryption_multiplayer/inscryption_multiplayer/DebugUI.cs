using DiskCardGame;
using inscryption_multiplayer.Networking;
using inscryption_multiplayer.Patches;
using Pixelplacement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static inscryption_multiplayer.Utils;
using Object = UnityEngine.Object;

namespace inscryption_multiplayer
{
    public class DebugUI
    {
        public static bool debugEnabled = true;
        public static bool UIOpen = true;

        public static bool toggleInverseIsHost = false;
        public static bool inverseIsHost = false;

        public static bool toggleReceiveAllMessages = false;
        public static bool receiveAllMessages = false;

        public static bool sendCard = false;
        public static bool sendSlot = false;

        public static bool toggleSendToAll = false;
        public static bool sendToAll = true;

        public static string message = "";

        public static PlayableCard chosenCard;
        public static CardSlot chosenSlot;

        public static List<CardSlot> allSlotsPlusPlayerQueue
        {
            get
            {
                List<CardSlot> slots = Singleton<BoardManager>.Instance.AllSlotsCopy;
                slots.AddRange(Player_Backline.PlayerQueueSlots);
                return slots;
            }
        }

        public static List<CardSlot> allSlots
        {
            get
            {
                return Singleton<BoardManager>.Instance.AllSlotsCopy;
            }
        }

        //currently only works with Start Alone
        public static void OnGUI()
        {
            if (Plugin.MultiplayerActive && debugEnabled)
            {
                UIOpen = GUI.Toggle(new Rect(20f, 10f, 200f, 20f), UIOpen, "Multiplayer Debug Menu");
                if (UIOpen)
                {
                    toggleInverseIsHost = GUI.Button(new Rect(220f, 70f, 200f, 40f), InscryptionNetworking.Connection.IsHost ? "Is Host" : "Is Not Host");
                    if (toggleInverseIsHost)
                    {
                        inverseIsHost = !inverseIsHost;
                    }

                    toggleReceiveAllMessages = GUI.Button(new Rect(220f, 150f, 200f, 40f), receiveAllMessages ? "Receiving All Messages" : "Receiving Messages Normally");
                    if (toggleReceiveAllMessages)
                    {
                        receiveAllMessages = !receiveAllMessages;
                    }

                    sendCard = GUI.Toggle(new Rect(20f, 50f, 200f, 20f), sendCard, "Send Card");
                    bool setCard = GUI.Button(new Rect(20f, 70f, 200f, 40f), "Set Card");
                    if (setCard)
                    {
                        Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
                        Singleton<BoardManager>.Instance.StartCoroutine(chooseSlot(allSlotsPlusPlayerQueue, allSlotsPlusPlayerQueue.Where(x => x.Card != null).ToList(), PlayableCardSelected));
                    }

                    sendSlot = GUI.Toggle(new Rect(20f, 130f, 200f, 20f), sendSlot, "Send Slot");
                    bool setSlot = GUI.Button(new Rect(20f, 150f, 200f, 40f), "Set Slot");
                    if (setSlot)
                    {
                        Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
                        Singleton<BoardManager>.Instance.StartCoroutine(chooseSlot(allSlots, allSlots, SlotSelected));
                    }

                    toggleSendToAll = GUI.Button(new Rect(20f, 210f, 200f, 40f), sendToAll ? "Send To All" : "Send To Opponent");
                    if (toggleSendToAll)
                    {
                        sendToAll = !sendToAll;
                    }

                    message = GUI.TextField(new Rect(20f, 270f, 200f, 40f), message);

                    bool sendMessage = GUI.Button(new Rect(20f, 330f, 200f, 40f), "Send Message");
                    if (sendMessage)
                    {
                        object obj = null;
                        string sendToWhoString = sendToAll ? "bypasscheck " : "";
                        if (!sendCard && !sendSlot)
                        {
                            InscryptionNetworking.Connection.Send($"{sendToWhoString}{message}");
                            return;
                        }

                        if (chosenCard == null || chosenSlot == null)
                        {
                            Plugin.Log.LogInfo($"card is null {chosenCard == null}, slot is null {chosenSlot == null}");
                            return;
                        }

                        CardSlot slot = allSlots.First(x => x.Index == chosenSlot.Index && x.IsPlayerSlot != chosenSlot.IsPlayerSlot);
                        if (sendCard)
                        {
                            obj = CardToMPInfo(chosenCard, slot);
                        }
                        else if (sendSlot)
                        {
                            obj = SlotToMPInfo(slot);
                        }

                        InscryptionNetworking.Connection.SendJson($"{sendToWhoString}{message}", obj);
                    }
                }
            }
        }

        public static void PlayableCardSelected(CardSlot slot)
        {
            chosenCard = slot.Card;
        }

        public static void SlotSelected(CardSlot slot)
        {
            chosenSlot = slot;
        }

        public static CardSlot recentlySelected;
        public static GameObject instanceTarget;
        public static GameObject target = ResourceBank.Get<GameObject>("Prefabs/Cards/SpecificCardModels/CannonTargetIcon");
        public static IEnumerator chooseSlot(List<CardSlot> allTargets, List<CardSlot> validTargets, Action<CardSlot> targetChosen)
        {
            if (validTargets.Count > 0)
            {
                Singleton<ViewManager>.Instance.Controller.SwitchToControlMode(Singleton<BoardManager>.Instance.ChoosingSlotViewMode, false);

                yield return Singleton<BoardManager>.Instance.ChooseTarget(allTargets, validTargets, CardSelected, InvalidTargetSelected, CursorEnteredSlot, () => false, CursorType.Target);

                if (instanceTarget != null && SaveManager.SaveFile.IsPart1)
                {
                    Tween.LocalScale(instanceTarget.transform, Vector3.zero, 0.1f, 0f, Tween.EaseIn, Tween.LoopType.None, null, delegate ()
                    {
                        Object.Destroy(instanceTarget);
                    }, true);
                }
                if (recentlySelected != null)
                {
                    targetChosen(recentlySelected);
                }

                Singleton<ViewManager>.Instance.Controller.SwitchToControlMode(Singleton<BoardManager>.Instance.DefaultViewMode, false);
            }
        }
        public static void CardSelected(CardSlot slot)
        {
            recentlySelected = slot;
        }
        public static void InvalidTargetSelected(CardSlot slot)
        {
            if (slot.Card != null)
            {
                slot.Card.Anim.StrongNegationEffect();
            }
            AudioController.Instance.PlaySound2D("toneless_negate", MixerGroup.GBCSFX, 0.2f, 0f, null, null, null, null, false);
        }
        public static void CursorEnteredSlot(CardSlot slot)
        {
            if (SaveManager.SaveFile.IsPart1)
            {
                if (instanceTarget != null)
                {
                    GameObject inst = instanceTarget;
                    Tween.LocalScale(inst.transform, Vector3.zero, 0.1f, 0f, Tween.EaseIn, Tween.LoopType.None, null, delegate ()
                    {
                        UnityEngine.Object.Destroy(inst);
                    }, true);
                }
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(target, slot.transform);
                gameObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                gameObject.transform.localRotation = Quaternion.identity;
                instanceTarget = gameObject;
            }
        }
    }
}
