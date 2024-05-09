using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;
using inscryption_multiplayer.Networking;
using Pixelplacement;
using UnityEngine;

public class MultiplayerItemData
{
    public string prefabId;
    public string pickupSoundId;
    public string placedSoundId;
    public string examineSoundId;
    public float modelHeight;
    
    public int? TargetSlotIndex;
}

[HarmonyPatch]
public class Item_Sync
{
    [HarmonyPatch(typeof(ConsumableItemSlot), nameof(ConsumableItemSlot.ConsumeItem))]
    [HarmonyPrefix]
    public static void SendItemActivation(ConsumableItemSlot __instance)
    {
        if (__instance.Item is not TargetSlotItem)
            SendItemData(null, __instance.Item.Data, false);
    }

    [HarmonyPatch(typeof(TargetSlotItem), nameof(TargetSlotItem.ActivateSequence), MethodType.Enumerator)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SendTargetItem(IEnumerable<CodeInstruction> instructions,
        MethodBase __originalMethod)
    {
        var instanceField = AccessTools.Field(__originalMethod.ReflectedType, "<>4__this");
        var itemDataGetter = AccessTools.PropertyGetter(typeof(Item), nameof(Item.Data));
        var targetField =
            AccessTools.Field(AccessTools.Inner(typeof(TargetSlotItem), "<>c__DisplayClass13_0"), "target");
        return new CodeMatcher(instructions)
            .MatchForward(true, new CodeMatch(OpCodes.Ldfld, targetField), new CodeMatch(OpCodes.Ldnull))
            .Insert(new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, instanceField),
                new CodeInstruction(OpCodes.Callvirt, itemDataGetter),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Item_Sync), nameof(SendItemData))))
            .InstructionEnumeration();
    }

    private static void SendItemData(CardSlot slot, ItemData itemData, bool targetItem)
    {
        if (!Plugin.MultiplayerActive || (targetItem && slot == null))
            return;
        InscryptionNetworking.Connection.SendJson(NetworkingMessage.ItemUsed, new MultiplayerItemData
        {
            prefabId = itemData.prefabId,
            pickupSoundId = itemData.pickupSoundId,
            placedSoundId = itemData.placedSoundId,
            examineSoundId = itemData.examineSoundId,
            modelHeight = itemData.modelHeight,
            TargetSlotIndex = slot?.Index
        });
    }

    [HarmonyPatch(typeof(ScreenColorEffect), nameof(ScreenColorEffect.SetColor))]
    [HarmonyPatch(typeof(ScreenColorEffect), nameof(ScreenColorEffect.SetIntensity))]
    [HarmonyPatch]
    [HarmonyPrefix]
    private static bool PreventScreenColor()
    {
        return !Plugin.MultiplayerActive || !MultiplayerRunState.Run.OpponentItemUsed;
    }

    public static IEnumerator HandleOpponentItem(MultiplayerItemData _itemData)
    {
        MultiplayerRunState.Run.OpponentItemSlot ??= GameObject.Find("ItemSlot_4").GetComponent<SelectableItemSlot>();
        var itemData = ScriptableObject.CreateInstance<ItemData>();
        itemData.prefabId = _itemData.prefabId;
        itemData.pickupSoundId = _itemData.pickupSoundId;
        itemData.placedSoundId = _itemData.placedSoundId;
        itemData.examineSoundId = _itemData.examineSoundId;
        itemData.modelHeight = _itemData.modelHeight;
        var itemSlot = MultiplayerRunState.Run.OpponentItemSlot;
        itemSlot.CreateItem(itemData);
        itemSlot.Selectable = false;
        yield return new WaitForSecondsRealtime(1f);
        var consumableItem = (ConsumableItem)itemSlot.Item;
        MultiplayerRunState.Run.OpponentItemUsed = true;
        if (_itemData.TargetSlotIndex != null && consumableItem is TargetSlotItem targetItem)
        {
            GetTargetItemOffset(consumableItem, out var positionOffset, out var rotationOffset);
            var targetSlot = Singleton<BoardManager>.Instance.playerSlots[(int)_itemData.TargetSlotIndex];
            targetItem.PlayExitAnimation();
            yield return new WaitForSeconds(0.1f);
            Singleton<UIManager>.Instance.Effects.GetEffect<EyelidMaskEffect>().SetIntensity(0.6f, 0.2f);
            Singleton<ViewManager>.Instance.SwitchToView(targetItem.SelectionView);
            yield return new WaitForSeconds(0.25f);
            var firstPersonItem = Singleton<FirstPersonController>.Instance.AnimController
                .SpawnFirstPersonAnimation(targetItem.FirstPersonPrefabId).transform;
            firstPersonItem.localPosition = targetItem.FirstPersonItemPos + Vector3.right * 3f;
            firstPersonItem.localEulerAngles = targetItem.FirstPersonItemEulers + rotationOffset;
            Singleton<InteractionCursor>.Instance.InteractionDisabled = false;
            Tween.Position(firstPersonItem,
                new Vector3(targetSlot.transform.position.x, firstPersonItem.position.y, firstPersonItem.position.z) +
                positionOffset, 0.2f, 0f, Tween.EaseOut);
            //targetItem.MoveItemToPosition(firstPersonItem, targetSlot.transform.position + new Vector3(.4f, 0f, 2f));
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;
            Singleton<InteractionCursor>.Instance.InteractionDisabled = true;
            yield return targetItem.OnValidTargetSelected(targetSlot, firstPersonItem.gameObject);
            UnityEngine.Object.Destroy(firstPersonItem.gameObject);
            Singleton<UIManager>.Instance.Effects.GetEffect<EyelidMaskEffect>().SetIntensity(0f, 0.2f);
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
        }
        else yield return consumableItem.ActivateSequence();
        MultiplayerRunState.Run.OpponentItemUsed = false;
    }

    private static void GetTargetItemOffset(ConsumableItem item, out Vector3 positionOffset, out Vector3 rotationOffset)
    {
        if (item is TrapperKnifeItem)
        {
            positionOffset = new(1f, 0f, 2.4f);
            rotationOffset = new(-240f, 180f, 35f);
            return;
        }
        
        if (item is ScissorsItem)
        {
            positionOffset = new(0.4f, 0f, 1.1f);
            rotationOffset = new(180f, 0f, 0f);
            return;
        }

        if (item is FishHookItem)
        {
            positionOffset = new(0.4f, 2f, 1.6f);
            rotationOffset = new(0f, 0f, 180f);
            return;
        }
        

        positionOffset = Vector3.zero;
        rotationOffset = Vector3.zero;
    }

    [HarmonyPatch(typeof(FirstPersonAnimationController), nameof(FirstPersonAnimationController.SpawnFirstPersonAnimation))]
    [HarmonyPrefix]
    private static bool PatchedAnimationSpawn(FirstPersonAnimationController __instance, string prefabName, Action<int> keyframesCallback, ref GameObject __result)
    {
        if (!Plugin.MultiplayerActive || !MultiplayerRunState.Run.OpponentItemUsed)
            return true;
        var gameObject = UnityEngine.Object.Instantiate(ResourceBank.Get<GameObject>("Prefabs/FirstPersonAnimations/" + prefabName));
        gameObject.transform.SetParent(__instance.pixelCamera.transform);
        GetAnimationItemOffset(prefabName, out var positionOffset, out var rotationOffset, out var endAnimation);
        if (endAnimation)
            __instance.StartCoroutine(ItemEndAnimation(gameObject));
        gameObject.transform.localPosition = positionOffset;
        gameObject.transform.localRotation = rotationOffset;
        if (keyframesCallback != null)
        {
            var componentInChildren = gameObject.GetComponentInChildren<FirstPersonAnimatorObject>();
            if (componentInChildren != null)
                componentInChildren.KeyframeCallback += keyframesCallback;
        }
        __result = gameObject;
        return false;
    }

    private static IEnumerator ItemEndAnimation(GameObject item)
    {
        var animator = item.GetComponentInChildren<Animator>();
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
        Tween.Position(item.transform, item.transform.position + new Vector3(0f, 10f, 0f), 1f, 0f, Tween.EaseOut);
    }
    
    private static void GetAnimationItemOffset(string prefabName, out Vector3 positionOffset,
        out Quaternion rotationOffset, out bool endAnimation)
    {
        endAnimation = true;
        switch (prefabName)
        {
            case "PliersAnimation":
                positionOffset = new(-0.2f, 0.9845f, 5.544f);
                rotationOffset = Quaternion.Euler(0f, 90f, 200f);
                return;
            case "BirdLegFanAnimation":
                positionOffset = new(-1f, 1f, 0f);
                rotationOffset = Quaternion.Euler(0f, 0f, 180f);
                return;
            case "FirstPersonBleachBrush":
                positionOffset = new Vector3(0f, -2f, 0f);
                rotationOffset = Quaternion.Euler(0f, 0f, 0f);
                return;
        }
        positionOffset = Vector3.zero;
        rotationOffset = Quaternion.identity;
        endAnimation = false;
    }
}
