using DiskCardGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class Sigil_Patches
    {
        [HarmonyPatch(typeof(DrawCreatedCard), nameof(DrawCreatedCard.CreateDrawnCard))]
        [HarmonyPrefix]
        public static bool DrawCreatedCardSync(ref DrawCreatedCard __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GainBattery), nameof(GainBattery.OnResolveOnBoard))]
        [HarmonyPrefix]
        public static bool GainBatterySync(ref GainBattery __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(QuadrupleBones), nameof(QuadrupleBones.OnDie))]
        [HarmonyPrefix]
        public static bool QuadrupleBonesSync(ref QuadrupleBones __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(BoneDigger), nameof(BoneDigger.OnTurnEnd))]
        [HarmonyPrefix]
        public static bool BoneDiggerSync(ref BoneDigger __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Tutor), nameof(Tutor.OnResolveOnBoard))]
        [HarmonyPrefix]
        public static bool TutorSync(ref Tutor __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(RandomConsumable), nameof(RandomConsumable.OnResolveOnBoard))]
        [HarmonyPrefix]
        public static bool RandomConsumableSync(ref RandomConsumable __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Morsel), nameof(Morsel.OnSacrifice))]
        [HarmonyPrefix]
        public static bool MorselSync(ref RandomConsumable __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }
    }
}
