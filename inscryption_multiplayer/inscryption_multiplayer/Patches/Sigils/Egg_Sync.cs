using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using UnityEngine;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Egg_Sync
    {
        private static WaitForSecondsRealtime WaitForEgg(CreateEgg instance) => Plugin.MultiplayerActive &&
            instance.Card.OpponentCard && instance.Card.Slot.opposingSlot.Card == null
                ? MultiplayerRunState.Run.EggCommunicator.CreateReceiver(waitText: "Waiting for egg")
                : new WaitForSecondsRealtime(.1f);
        
        private static bool ModifyEgg(object receiver, bool ravenEgg, CreateEgg instance)
        {
            if (!Plugin.MultiplayerActive || !instance.Card.OpponentCard)
                return ravenEgg;
            return ((SigilCommunicator<bool>.SigilReceiver)receiver).Data;
        }
        
        private static void SendEgg(bool ravenEgg, CreateEgg instance)
        {
            if (Plugin.MultiplayerActive && !instance.Card.OpponentCard)
                MultiplayerRunState.Run.EggCommunicator.Send(ravenEgg);
        }

        [HarmonyPatch(typeof(CreateEgg), nameof(CreateEgg.OnResolveOnBoard), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SyncEgg(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var instanceField = AccessTools.Field(__originalMethod.ReflectedType, "<>4__this");
            var ravenEggField = AccessTools.Field(__originalMethod.ReflectedType, "<ravenEgg>5__3");
            var currentField = AccessTools.Field(__originalMethod.ReflectedType, "<>2__current");
            var skip = false;
            var replacedWait = false;
            foreach (var instruction in instructions)
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }
                if (!replacedWait && instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == .1f)
                {
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, instanceField);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Egg_Sync), nameof(WaitForEgg)));
                    skip = true;
                    replacedWait = true;
                    continue;
                }
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld && (FieldInfo)instruction.operand == ravenEggField)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, currentField);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, ravenEggField);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, instanceField);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Egg_Sync), nameof(ModifyEgg)));
                    yield return new CodeInstruction(OpCodes.Stfld, ravenEggField);
                    
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, ravenEggField);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, instanceField);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Egg_Sync), nameof(SendEgg)));
                }
            }
        }
    }
}
