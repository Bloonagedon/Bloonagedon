using BepInEx;
using BepInEx.Configuration;
using DiskCardGame;
using GBC;
using HarmonyLib;
using inscryption_multiplayer.Networking;
using InscryptionAPI.Card;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace inscryption_multiplayer.Patches
{
    [HarmonyPatch]
    public class LifeCounter_Patches
    {
        public static GameObject LifeCounterObject;
        public static PlayableCard LifeCounterCard;
        public static CardInfo LifeCounterCardInfo;

        public static int? OldBalance = null;

        public static CardModificationInfo nameReplacementMod = new CardModificationInfo();

        public class LifeCounterBackground : CardAppearanceBehaviour
        {
            // Token: 0x060000AA RID: 170 RVA: 0x000091A4 File Offset: 0x000073A4
            public override void ApplyAppearance()
            {
                base.Card.RenderInfo.baseTextureOverride = GetTexture("Life_Counter.png");
            }

            // Token: 0x04000040 RID: 64
            public static CardAppearanceBehaviour.Appearance CustomAppearance;
        }

        public static void UpdateLifeCounter()
        {
            string name = $"{GameSettings.Current.ScaleSize + LifeManager.Instance.Balance}/{GameSettings.Current.ScaleSize - LifeManager.Instance.Balance}";
            nameReplacementMod.nameReplacement = name;
            LifeCounterCard.RenderCard();
        }

        public static Texture2D GetTexture(string path)
        {
            byte[] imgBytes = File.ReadAllBytes(Path.Combine(Plugin.Directory, path));
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgBytes);
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        [HarmonyPatch(typeof(TurnManager), "LifeLossConditionsMet")]
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, ref TurnManager __instance)
        {
            __result = Mathf.Abs(Singleton<LifeManager>.Instance.Balance) >= GameSettings.Current.ScaleSize || __instance.opponent.Surrendered || __instance.PlayerSurrendered;
            return false;
        }

        [HarmonyPatch(typeof(CardDrawPiles), nameof(CardDrawPiles.Initialize))]
        [HarmonyPostfix]
        public static void InitializePostfix()
        {
            Plugin.Log.LogInfo("huiqriuggiyurq");
            if (!Plugin.MultiplayerActive || GameSettings.Current.ScaleSize == 5)
            {
                return;
            }

            LifeCounterObject = UnityEngine.Object.Instantiate<GameObject>(Singleton<CardSpawner>.Instance.playableCardPrefab);
            LifeCounterCard = LifeCounterObject.GetComponent<PlayableCard>();
            LifeCounterCard.SetInfo(LifeCounterCardInfo);
            UpdateLifeCounter();
            Vector3 position = new Vector3(-2.87f, 6.83f, -0.6f);
            LifeCounterObject.transform.localPosition = position;
            Quaternion rotation = Quaternion.Euler(15, 325, 0);
            LifeCounterObject.transform.localRotation = rotation;
            LifeCounterObject.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        }

        [HarmonyPatch(typeof(CardDrawPiles), nameof(CardDrawPiles.CleanUp))]
        [HarmonyPostfix]
        public static void CleanUpPostfix()
        {
            if (LifeCounterObject != null)
            {
                UnityEngine.Object.Destroy(LifeCounterObject.gameObject);
            }
        }

        [HarmonyPatch(typeof(CombatPhaseManager), nameof(CombatPhaseManager.DoCombatPhase))]
        [HarmonyPostfix]
        public static IEnumerator DoCombatPhasePostfix(IEnumerator enumerator, CombatPhaseManager __instance, bool playerIsAttacker, SpecialBattleSequencer specialSequencer)
        {
            __instance.DamageDealtThisPhase = 0;
            List<CardSlot> attackingSlots = playerIsAttacker ? Singleton<BoardManager>.Instance.PlayerSlotsCopy : Singleton<BoardManager>.Instance.OpponentSlotsCopy;
            attackingSlots.RemoveAll((CardSlot x) => x.Card == null || x.Card.Attack == 0);
            bool atLeastOneAttacker = attackingSlots.Count > 0;
            yield return __instance.InitializePhase(attackingSlots, playerIsAttacker);
            if (specialSequencer != null)
            {
                if (playerIsAttacker)
                {
                    yield return specialSequencer.PlayerCombatStart();
                }
                else
                {
                    yield return specialSequencer.OpponentCombatStart();
                }
            }
            if (atLeastOneAttacker)
            {
                bool attackedWithSquirrel = false;
                foreach (CardSlot cardSlot in attackingSlots)
                {
                    cardSlot.Card.AttackedThisTurn = false;
                    if (cardSlot.Card.Info.IsOfTribe(Tribe.Squirrel))
                    {
                        attackedWithSquirrel = true;
                    }
                }
                foreach (CardSlot cardSlot2 in attackingSlots)
                {
                    if (cardSlot2.Card != null && !cardSlot2.Card.AttackedThisTurn)
                    {
                        cardSlot2.Card.AttackedThisTurn = true;
                        yield return __instance.SlotAttackSequence(cardSlot2);
                    }
                }
                List<CardSlot>.Enumerator enumerator2 = default(List<CardSlot>.Enumerator);
                if (specialSequencer != null && playerIsAttacker)
                {
                    yield return specialSequencer.PlayerCombatPostAttacks();
                }
                if (__instance.DamageDealtThisPhase > 0)
                {
                    yield return new WaitForSeconds(0.4f);
                    yield return __instance.VisualizeDamageMovingToScales(playerIsAttacker);
                    int excessDamage = 0;
                    if (playerIsAttacker)
                    {
                        excessDamage = Singleton<LifeManager>.Instance.Balance + __instance.DamageDealtThisPhase - GameSettings.Current.ScaleSize;
                        if (attackedWithSquirrel && excessDamage >= 0)
                        {
                            AchievementManager.Unlock(Achievement.PART1_SPECIAL1);
                        }
                        excessDamage = Mathf.Max(0, excessDamage);
                    }
                    int damage = __instance.DamageDealtThisPhase - excessDamage;
                    AscensionStatsData.TryIncreaseStat(AscensionStat.Type.MostDamageDealt, __instance.DamageDealtThisPhase);
                    if (__instance.DamageDealtThisPhase >= 666)
                    {
                        AchievementManager.Unlock(Achievement.PART2_SPECIAL2);
                    }
                    if (!(specialSequencer != null) || !specialSequencer.PreventDamageAddedToScales)
                    {
                        if (damage > 0)
                        {
                            yield return Singleton<LifeManager>.Instance.ShowDamageSequence(damage, damage, !playerIsAttacker, 0f, null, 0f, true);
                        }
                        else if (damage < 0)
                        {
                            yield return Singleton<LifeManager>.Instance.ShowDamageSequence(-damage, -damage, playerIsAttacker, 0f, null, 0f, true);
                        }
                    }
                    if (specialSequencer != null)
                    {
                        yield return specialSequencer.DamageAddedToScale(damage + excessDamage, playerIsAttacker);
                    }
                    if ((!(specialSequencer != null) || !specialSequencer.PreventDamageAddedToScales) && excessDamage > 0 && Singleton<TurnManager>.Instance.Opponent.NumLives == 1 && Singleton<TurnManager>.Instance.Opponent.GiveCurrencyOnDefeat)
                    {
                        yield return Singleton<TurnManager>.Instance.Opponent.TryRevokeSurrender();
                        RunState.Run.currency += excessDamage;
                        yield return __instance.VisualizeExcessLethalDamage(excessDamage, specialSequencer);
                    }
                }

                yield return new WaitForSeconds(0.15f);
                foreach (CardSlot cardSlot3 in attackingSlots)
                {
                    if (cardSlot3.Card != null && cardSlot3.Card.TriggerHandler.RespondsToTrigger(Trigger.AttackEnded, Array.Empty<object>()))
                    {
                        yield return cardSlot3.Card.TriggerHandler.OnTrigger(Trigger.AttackEnded, Array.Empty<object>());
                    }
                }
                enumerator2 = default(List<CardSlot>.Enumerator);
            }
            if (specialSequencer != null)
            {
                if (playerIsAttacker)
                {
                    yield return specialSequencer.PlayerCombatEnd();
                }
                else
                {
                    yield return specialSequencer.OpponentCombatEnd();
                }
            }
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
            if (atLeastOneAttacker)
            {
                yield return new WaitForSeconds(0.15f);
            }
            yield break;
        }
    }
}
