using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using DiskCardGame;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Map_Patches
    {
        [HarmonyPatch(typeof(CardBattleNodeData), MethodType.Constructor)]
        [HarmonyPostfix]
        public static void ApplyMultiplayerBattleSequencer(CardBattleNodeData __instance)
        {
            if(Plugin.MultiplayerActive)
                __instance.specialBattleId = nameof(Multiplayer_Battle_Sequencer);
        }
        
        [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.ChooseSpecialNodeFromPossibilities))]
        [HarmonyPrefix]
        public static void RemoveBuildTotemNodeData(ref List<NodeData> possibilities)
        {
            if(Plugin.MultiplayerActive)
                possibilities.RemoveAll(n => n is BuildTotemNodeData);
        }

        private static NodeData CreateNodeData()
        {
            return Plugin.MultiplayerActive ? new CardBattleNodeData() : new TotemBattleNodeData();
        }

        [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.CreateNode))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RemoveTotemBattleNodeData(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var constructor = AccessTools.Constructor(typeof(TotemBattleNodeData));
            var instruction = instructions.FirstOrDefault(inst => inst.opcode == OpCodes.Newobj && inst.operand as ConstructorInfo == constructor);
            if (instruction is not null)
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(Map_Patches), nameof(CreateNodeData));
            }
            return instructions;
        }
    }
}