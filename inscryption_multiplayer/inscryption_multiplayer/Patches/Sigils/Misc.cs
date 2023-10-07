using DiskCardGame;
using HarmonyLib;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class Sigil_Misc
    {
        [HarmonyPatch(typeof(DrawCreatedCard), nameof(DrawCreatedCard.CreateDrawnCard))]
        [HarmonyPatch(typeof(GainBattery), nameof(GainBattery.OnResolveOnBoard))]
        [HarmonyPatch(typeof(QuadrupleBones), nameof(QuadrupleBones.OnDie))]
        [HarmonyPatch(typeof(BoneDigger), nameof(BoneDigger.OnTurnEnd))]
        [HarmonyPatch(typeof(Tutor), nameof(Tutor.OnResolveOnBoard))]
        [HarmonyPatch(typeof(RandomConsumable), nameof(RandomConsumable.OnResolveOnBoard))]
        [HarmonyPatch(typeof(Morsel), nameof(Morsel.OnSacrifice))]
        [HarmonyPrefix]
        public static bool DrawCreatedCardSync(AbilityBehaviour __instance)
        {
            if (Plugin.MultiplayerActive && __instance.Card.OpponentCard)
            {
                return false;
            }
            return true;
        }
    }
}
