using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer;
using inscryption_multiplayer.Networking;
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
    public static IEnumerable<CodeInstruction> SendTargetItem(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
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
		if(!Plugin.MultiplayerActive || (targetItem && slot == null))
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
            var targetSlot = Singleton<BoardManager>.Instance.playerSlots[(int)_itemData.TargetSlotIndex];
            targetItem.PlayExitAnimation();
            yield return new WaitForSeconds(0.1f);
            Singleton<UIManager>.Instance.Effects.GetEffect<EyelidMaskEffect>().SetIntensity(0.6f, 0.2f);
            Singleton<ViewManager>.Instance.SwitchToView(targetItem.SelectionView);
            yield return new WaitForSeconds(0.25f);
            var firstPersonItem = Singleton<FirstPersonController>.Instance.AnimController
                .SpawnFirstPersonAnimation(targetItem.FirstPersonPrefabId).transform;
            firstPersonItem.localPosition = targetItem.FirstPersonItemPos + Vector3.right * 3f;
            firstPersonItem.localEulerAngles = targetItem.FirstPersonItemEulers;
            Singleton<InteractionCursor>.Instance.InteractionDisabled = false;
            targetItem.MoveItemToPosition(firstPersonItem, targetSlot.transform.position);
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;
            Singleton<InteractionCursor>.Instance.InteractionDisabled = true;
            yield return targetItem.OnValidTargetSelected(targetSlot, firstPersonItem.gameObject);
            Object.Destroy(firstPersonItem.gameObject);
            Singleton<UIManager>.Instance.Effects.GetEffect<EyelidMaskEffect>().SetIntensity(0f, 0.2f);
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
        }
        else yield return consumableItem.ActivateSequence();
        MultiplayerRunState.Run.OpponentItemUsed = false;
    }
}
