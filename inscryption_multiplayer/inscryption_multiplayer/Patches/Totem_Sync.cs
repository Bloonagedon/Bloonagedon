using DiskCardGame;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using System.Collections;
using UnityEngine;

namespace inscryption_multiplayer
{
    [HarmonyPatch]
    public class Totem_Sync
    {
        [HarmonyPatch(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.BuildPhase))]
        [HarmonyPostfix]
        public static void RecordBuildPhase(ref IEnumerator __result)
        {
            __result = Utils.CallbackRoutine(__result, () =>
            {
                if (RunState.Run.totems.Count > 0)
                    InscryptionNetworking.Connection.SendJson(NetworkingMessage.ChangeOpponentTotem, RunState.Run.totems[0]);
            });
        }

        [HarmonyPatch(typeof(TotemOpponent), nameof(TotemOpponent.IntroSequence))]
        [HarmonyPrefix]
        public static void SetEncounterTotem(ref EncounterData encounter)
        {
            if (!Plugin.MultiplayerActive)
                return;
            encounter.opponentType = Opponent.Type.Totem;
            var opponentTotem = new TotemItemData();
            opponentTotem.top = new TotemTopData(MultiplayerRunState.Run.OpponentTotem.tribe);
            opponentTotem.bottom = new TotemBottomData();
            opponentTotem.bottom.effectParams = new TotemBottomData.EffectParameters();
            opponentTotem.bottom.effectParams.ability = MultiplayerRunState.Run.OpponentTotem.ability;
            encounter.opponentTotem = opponentTotem;
        }

        public static void ApplyTotem()
        {
            if (MultiplayerRunState.Run.OpponentTotem != null)
            {
                var opponent = Singleton<TurnManager>.Instance.Opponent;
                var opponentObject = opponent.gameObject;
                var newOpponent = opponentObject.AddComponent<TotemOpponent>();
                newOpponent.AI = opponent.AI;
                newOpponent.NumLives = opponent.NumLives;
                newOpponent.OpponentType = Opponent.Type.Totem;
                newOpponent.TurnPlan = opponent.TurnPlan;
                newOpponent.Blueprint = opponent.Blueprint;
                newOpponent.Difficulty = opponent.Difficulty;
                newOpponent.ExtraTurnsToSurrender = opponent.ExtraTurnsToSurrender;
                Singleton<TurnManager>.Instance.opponent = newOpponent;
                Object.DestroyImmediate(opponent);
            }
        }
    }
}
