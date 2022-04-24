using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ReinforcedMechanoids
{

    [StaticConstructorOnStartup]
    public static class Core
    {
        static Core()
        {
            var harm = new Harmony("ReinforcedMechanoids.Mod");
            harm.PatchAll();
            ReinforcedMechanoidsMod.ApplySettings();
        }

        public static BodyPartRecord GetNonMissingBodyPart(Pawn pawn, BodyPartDef def)
        {
            foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (notMissingPart.def == def)
                {
                    return notMissingPart;
                }
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(JobGiver_AITrashColonyClose), "TryGiveJob")]
    public static class JobGiver_AITrashColonyClose_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            return JobGiver_AIFightEnemy_TryGiveJob.TryModifyJob(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_AISapper), "TryGiveJob")]
    public static class JobGiver_AISapper_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            return JobGiver_AIFightEnemy_TryGiveJob.TryModifyJob(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob")]
    public static class JobGiver_AIGotoNearestHostile_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            return JobGiver_AIFightEnemy_TryGiveJob.TryModifyJob(pawn, ref __result);
        }
    }

    [HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
    public static class JobGiver_AITrashBuildingsDistant_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            return JobGiver_AIFightEnemy_TryGiveJob.TryModifyJob(pawn, ref __result);
        }
    }

    public class JobGiver_AIFightEnemiesNearToCaretaker : JobGiver_AIFightEnemies
    {
        public static Pawn caretaker;
        public override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            return caretaker.Position.DistanceTo(target.Position) <= 10;
        }
    }

    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class JobGiver_AIFightEnemy_TryGiveJob
    {
        public static bool Prefix(JobGiver_AIFightEnemy __instance, Pawn pawn, ref Job __result)
        {
            if (!(__instance is JobGiver_AIFightEnemiesNearToCaretaker))
            {
                return TryModifyJob(pawn, ref __result);
            }
            return true;
        }

        public static bool TryModifyJob(Pawn pawn, ref Job __result)
        {
            if (pawn.RaceProps.IsMechanoid && pawn.kindDef != RM_DefOf.RM_Mech_Caretaker)
            {
                var otherPawns = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)
                    .Where(x => x.RaceProps.IsMechanoid && !x.Fogged() && !x.Dead && x.Awake()).Except(pawn)
                    .OrderBy(x => x.Position.DistanceTo(pawn.Position)).ToList();
                if (otherPawns.Any(x => x.kindDef == RM_DefOf.RM_Mech_Caretaker))
                {
                    var firstCloseCaretaker = otherPawns.Where(x => x.kindDef == RM_DefOf.RM_Mech_Caretaker)
                        .OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                    if (pawn.kindDef == RM_DefOf.RM_Mech_Vulture)
                    {
                        __result = HealOtherMechanoids(pawn, otherPawns);
                        if (__result is null)
                        {
                            var followJob = TryGiveFollowJob(pawn, firstCloseCaretaker, 6);
                            if (followJob != null)
                            {
                                followJob.locomotionUrgency = LocomotionUrgency.Amble;
                                __result = followJob;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        var fightEnemiesJobGiver = new JobGiver_AIFightEnemiesNearToCaretaker();
                        JobGiver_AIFightEnemiesNearToCaretaker.caretaker = firstCloseCaretaker;
                        fightEnemiesJobGiver.ResolveReferences();
                        fightEnemiesJobGiver.targetAcquireRadius = 12f;
                        fightEnemiesJobGiver.targetKeepRadius = 12f;
                        var fightEnemiesJob = fightEnemiesJobGiver.TryGiveJob(pawn);
                        JobGiver_AIFightEnemiesNearToCaretaker.caretaker = null;
                        if (fightEnemiesJob != null)
                        {
                            __result = fightEnemiesJob;
                            return false;
                        }
                        var nearestCell = JobGiver_WalkToPlayerBase.GetNearestCellToPlayerBase(firstCloseCaretaker, out var centerColony, out var firstBlockingBuilding);
                        if (firstBlockingBuilding != null && firstBlockingBuilding.Position.DistanceTo(pawn.Position) <= 10)
                        {
                            __result = MeleeAttackJob(firstBlockingBuilding);
                            if (__result != null)
                            {
                                return false;
                            }
                        }

                        if (firstCloseCaretaker.CurJobDef == JobDefOf.Wait)
                        {
                            return true;
                        }
                        else
                        {
                            var followJob = TryGiveFollowJob(pawn, firstCloseCaretaker, 6);
                            if (followJob != null)
                            {
                                followJob.locomotionUrgency = LocomotionUrgency.Amble;
                                __result = followJob;
                                return false;
                            }
                        }
                    }
                }

                if (pawn.kindDef == RM_DefOf.RM_Mech_Vulture)
                {
                    __result = HealOtherMechanoids(pawn, otherPawns);
                    if (__result is null)
                    {
                        __result = FollowOtherMechanoids(pawn, otherPawns);
                    }
                    return false;
                }
            }
            return true;
        }
        public static readonly IntRange ExpiryInterval_ShooterSucceeded = new IntRange(450, 550);

        public static readonly IntRange ExpiryInterval_Melee = new IntRange(360, 480);
        private static Job MeleeAttackJob(Thing enemyTarget)
        {
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, enemyTarget);
            job.expiryInterval = ExpiryInterval_Melee.RandomInRange;
            job.checkOverrideOnExpire = true;
            job.expireRequiresEnemiesNearby = true;
            return job;
        }

        public static Job HealOtherMechanoids(Pawn pawn, List<Pawn> otherPawns)
        {
            foreach (var otherPawn in otherPawns)
            {
                if (otherPawn.CanBeHealed() && pawn.CanReserveAndReach(otherPawn, PathEndMode.Touch, Danger.None))
                {
                    var job = JobMaker.MakeJob(RM_DefOf.RM_RepairMechanoid, otherPawn);
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    return job;
                }
            }

            var lord = pawn.GetLord();
            if (lord != null && lord.ownedBuildings != null)
            {
                foreach (var building in lord.ownedBuildings.OrderBy(x => x.Position.DistanceTo(pawn.Position)).ToList())
                {
                    if (building.Spawned && RepairUtility.PawnCanRepairNow(pawn, building) && pawn.CanReserve(building)
                        && pawn.CanReach(building, PathEndMode.Touch, Danger.None))
                    {
                        var job = JobMaker.MakeJob(RM_DefOf.RM_RepairThing, building);
                        job.locomotionUrgency = LocomotionUrgency.Sprint;
                        return job;
                    }
                }
            }
            return null;
        }
        public static Job FollowOtherMechanoids(Pawn pawn, List<Pawn> otherPawns)
        {
            foreach (var otherPawn2 in otherPawns.InRandomOrder())
            {
                if (RM_DefOf.RM_FollowClose != otherPawn2.CurJobDef && otherPawn2.kindDef != RM_DefOf.RM_Mech_Vulture)
                {
                    var job = TryGiveFollowJob(pawn, otherPawn2, 12);
                    if (job != null)
                    {
                        return job;
                    }
                }
            }
            return null;
        }

        private static bool CanBeHealed(this Pawn pawn)
        {
            return pawn.kindDef != RM_DefOf.RM_Mech_Vulture && pawn.health.hediffSet.hediffs.Any(x => x is Hediff_Injury);
        }

        private static Job TryGiveFollowJob(Pawn pawn, Pawn followee, float radius)
        {
            if (!followee.Spawned || !pawn.CanReach(followee, PathEndMode.Touch, Danger.None))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(RM_DefOf.RM_FollowClose, followee);
            job.expiryInterval = 140;
            job.checkOverrideOnExpire = true;
            job.followRadius = radius;
            return job;
        }
    }

    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.DirtyCache))]
    public static class DirtyCache_Patch
    {
        private static void Postfix(HediffSet __instance)
        {
            var comp = __instance.pawn.GetComp<CompChangePawnGraphic>();
            if (comp != null)
            {
                comp.TryChangeGraphic(false);
            }
        }
    }

    [HarmonyPatch(typeof(DamageWorker_AddInjury), "GetExactPartFromDamageInfo")]
    public static class GetExactPartFromDamageInfo_Patch
    {
        public static bool pickShield;
        private static void Prefix(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.Instigator is Pawn attacker && pawn.kindDef == RM_DefOf.RM_Mech_Behemoth)
            {
                float angle = (attacker.DrawPos - pawn.DrawPos).AngleFlat();
                var rot = Pawn_RotationTracker.RotFromAngleBiased(angle);
                if (rot == pawn.Rotation && Core.GetNonMissingBodyPart(pawn, RM_DefOf.RM_BehemothShield) != null)
                {
                    pickShield = true;
                }
            }
        }
        private static void Postfix(DamageInfo dinfo, Pawn pawn)
        {
            pickShield = false;
        }
    }

    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.GetRandomNotMissingPart))]
    public static class GetRandomNotMissingPart_Patch
    {
        private static void Postfix(HediffSet __instance, ref BodyPartRecord __result)
        {
            if (GetExactPartFromDamageInfo_Patch.pickShield && Rand.Chance(0.8f))
            {
                var part = Core.GetNonMissingBodyPart(__instance.pawn, RM_DefOf.RM_BehemothShield);
                if (part != null)
                {
                    __result = part;
                }
            }
        }
    }

    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    //public static class TryGetAttackVerb_Patch
    //{
    //    private static void Postfix(Pawn __instance, ref Verb __result)
    //    {
    //        if (__instance.kindDef == RM_DefOf.RM_Mech_Vulture)
    //        {
    //            __result = null;
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(MechClusterGenerator), nameof(MechClusterGenerator.MechKindSuitableForCluster))]
    public class MechSpawn_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(PawnKindDef __0, ref bool __result)
        {
            if (__0 == RM_DefOf.RM_Mech_Caretaker)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb")]
    public static class Patch_Pawn_MeleeVerbs_ChooseMeleeVerb
    {
        public static bool Prefix(Pawn_MeleeVerbs __instance, Thing target)
        {
            if (__instance.pawn.kindDef == RM_DefOf.RM_Mech_Behemoth)
            {
                var part = Core.GetNonMissingBodyPart(__instance.pawn, RM_DefOf.RM_BehemothShield);
                if (part != null)
                {
                    var verb = __instance.GetUpdatedAvailableVerbsList(false).Where(x => x.verb is Verb_MeleeAttackDamageBehemoth).FirstOrDefault();
                    if (verb.verb != null)
                    {
                        __instance.SetCurMeleeVerb(verb.verb, target);
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PawnTweener), "PreDrawPosCalculation")]
    public static class PawnTweener_PreDrawPosCalculation_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pawnField = AccessTools.Field(typeof(PawnTweener), "pawn");
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (i > 1 && codes[i - 1].opcode == OpCodes.Ldloc_1 && codes[i].opcode == OpCodes.Ldloc_2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, pawnField);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnTweener_PreDrawPosCalculation_Patch), "ReturnNum"));
                }
            }
        }

        public static float ReturnNum(float num, Pawn pawn)
        {
            if (!pawn.pather.moving && pawn.health.hediffSet.GetFirstHediffOfDef(RM_DefOf.RM_BehemothAttack) != null)
            {
                return num * 0.5f;
            }
            return num;
        }
    }
}

