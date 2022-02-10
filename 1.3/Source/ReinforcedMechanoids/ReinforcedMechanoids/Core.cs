using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
        }
        public static BodyPartRecord GetNonMissingBodyPart(Pawn pawn, BodyPartTagDef def)
        {
            foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (notMissingPart.def.tags.Contains(def))
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

    public class JobGiver_AIFightEnemiesNearToWalker : JobGiver_AIFightEnemies
    {
        public static Pawn walker;
        public override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            return walker.Position.DistanceTo(target.Position) <= 10;
        }
    }

    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class JobGiver_AIFightEnemy_TryGiveJob
    {
        public static bool Prefix(JobGiver_AIFightEnemy __instance, Pawn pawn, ref Job __result)
        {
            if (!(__instance is JobGiver_AIFightEnemiesNearToWalker))
            {
                return TryModifyJob(pawn, ref __result);
            }
            return true;
        }

        public static bool TryModifyJob(Pawn pawn, ref Job __result)
        {
            if (pawn.RaceProps.IsMechanoid && pawn.kindDef != RM_DefOf.RM_Mech_Walker)
            {
                var otherPawns = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)
                    .Where(x => x.RaceProps.IsMechanoid && !x.Fogged() && !x.Dead && x.Awake()).Except(pawn)
                    .OrderBy(x => x.Position.DistanceTo(pawn.Position)).ToList();
                if (otherPawns.Any(x => x.kindDef == RM_DefOf.RM_Mech_Walker))
                {
                    var firstCloseWalker = otherPawns.Where(x => x.kindDef == RM_DefOf.RM_Mech_Walker)
                        .OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                    if (pawn.kindDef == RM_DefOf.RM_Mech_Vulture)
                    {
                        if (pawn.mindState.meleeThreat != null)
                        {
                            __result = JobMaker.MakeJob(JobDefOf.AttackMelee, pawn.mindState.meleeThreat);
                            __result.maxNumMeleeAttacks = 1;
                            __result.expiryInterval = 200;
                            __result.reactingToMeleeThreat = true;
                        }
                        else
                        {
                            __result = HealOtherMechanoids(pawn, otherPawns);
                            if (__result is null)
                            {
                                var followJob = TryGiveFollowJob(pawn, firstCloseWalker, 6);
                                if (followJob != null)
                                {
                                    followJob.locomotionUrgency = LocomotionUrgency.Amble;
                                    __result = followJob;
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        var fightEnemiesJobGiver = new JobGiver_AIFightEnemiesNearToWalker();
                        JobGiver_AIFightEnemiesNearToWalker.walker = firstCloseWalker;
                        fightEnemiesJobGiver.ResolveReferences();
                        fightEnemiesJobGiver.targetAcquireRadius = 12f;
                        fightEnemiesJobGiver.targetKeepRadius = 12f;
                        var fightEnemiesJob = fightEnemiesJobGiver.TryGiveJob(pawn);
                        JobGiver_AIFightEnemiesNearToWalker.walker = null;
                        if (fightEnemiesJob != null)
                        {
                            __result = fightEnemiesJob;
                            return false;
                        }
                        var nearestCell = JobGiver_WalkToPlayerBase.GetNearestCellToPlayerBase(firstCloseWalker, out var centerColony, out var firstBlockingBuilding);
                        if (firstBlockingBuilding != null && firstBlockingBuilding.Position.DistanceTo(pawn.Position) <= 10)
                        {
                            __result = MeleeAttackJob(firstBlockingBuilding);
                            if (__result != null)
                            {
                                return false;
                            }
                        }

                        if (firstCloseWalker.CurJobDef == JobDefOf.Wait)
                        {
                            return true;
                        }
                        else
                        {
                            var followJob = TryGiveFollowJob(pawn, firstCloseWalker, 6);
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
                    if (pawn.mindState.meleeThreat != null)
                    {
                        __result = JobMaker.MakeJob(JobDefOf.AttackMelee, pawn.mindState.meleeThreat);
                        __result.maxNumMeleeAttacks = 1;
                        __result.expiryInterval = 200;
                        __result.reactingToMeleeThreat = true;
                    }
                    else
                    {
                        __result = HealOtherMechanoids(pawn, otherPawns);
                        if (__result is null)
                        {
                            __result = FollowOtherMechanoids(pawn, otherPawns);
                        }
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
                if (otherPawn.CanBeHealed() && pawn.CanReach(otherPawn, PathEndMode.Touch, Danger.None))
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
                    if (RepairUtility.PawnCanRepairNow(pawn, building) && pawn.CanReserve(building, 1, -1, null, true) && pawn.CanReach(building, PathEndMode.Touch, Danger.None))
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
                var job = TryGiveFollowJob(pawn, otherPawn2, 12);
                if (job != null)
                {
                    return job;
                }
            }
            return null;
        }

        private static bool CanBeHealed(this Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs.Any(x => x is Hediff_Injury);
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
}
