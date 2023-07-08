using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using UnityEngine;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Egg_Sync
    {
        public class WaitForEgg : WaitForSecondsRealtime
        {
            public override bool keepWaiting => base.keepWaiting || (WaitForQueue && MultiplayerRunState.Run.EggQueue.Count == 0);

            private readonly bool WaitForQueue;
            
            public WaitForEgg(CreateEgg instance) : base(.1f)
            {
                WaitForQueue = Plugin.MultiplayerActive && instance.Card.OpponentCard && instance.Card.Slot.opposingSlot == null;
            }
        }
        
        private static bool ModifyEgg(bool ravenEgg, CreateEgg instance)
        {
            if (!Plugin.MultiplayerActive || !instance.Card.OpponentCard)
                return ravenEgg;
            return MultiplayerRunState.Run.EggQueue.Dequeue();
        }
        
        private static void SendEgg(bool ravenEgg, CreateEgg instance)
        {
            if(Plugin.MultiplayerActive && !instance.Card.OpponentCard)
                InscryptionNetworking.Connection.Send($"{NetworkingMessage.EggPlaced} {(ravenEgg ? 1 : 0)}");
        }

        [HarmonyPatch(typeof(CreateEgg), nameof(CreateEgg.OnResolveOnBoard), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SyncEgg(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var instanceField = AccessTools.Field(__originalMethod.ReflectedType, "<>4__this");
            var ravenEggField = AccessTools.Field(__originalMethod.ReflectedType, "<ravenEgg>5__3");
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
                    yield return new CodeInstruction(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(WaitForEgg), new[] { typeof(CreateEgg) }));
                    skip = true;
                    replacedWait = true;
                    continue;
                }
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld && (FieldInfo)instruction.operand == ravenEggField)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Dup);
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
