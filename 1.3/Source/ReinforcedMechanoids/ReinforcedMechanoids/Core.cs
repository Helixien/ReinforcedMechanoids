using HarmonyLib;
using RimWorld;
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
            foreach (var typ in typeof(ThinkNode_JobGiver).AllSubclasses())
            {
                var method = AccessTools.Method(typ, "TryGiveJob");
                if (method != null && method.DeclaringType == typ)
                {
                    harm.Patch(method, postfix: new HarmonyMethod(AccessTools.Method(typeof(Core), nameof(Loggging))));
                }
            }
        }

        public static void Loggging(ThinkNode_JobGiver __instance, Job __result, Pawn pawn)
        {
            //if (pawn.RaceProps.IsMechanoid)
            //{
            //    Log.Message(pawn + " does search from " + __instance + " found " + __result);
            //}
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Pawn_JobTracker_StartJob
    {
        public static void Postfix(Pawn ___pawn, Job newJob)
        {
            //if (___pawn.RaceProps.IsMechanoid)
            //    Log.Message(___pawn + " is starting new job: " + newJob);
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "PatherFailed")]
    public static class Pawn_PathFollower_PatherFailed
    {
        public static void Prefix(Pawn ___pawn)
        {
            Log.Message(___pawn + " PATHER FAILED: " + ___pawn.CurJob);
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
            if (pawn.kindDef == RM_DefOf.RM_Mech_Walker)
            {
                pawn.jobs.debugLog = true;
            }
            if (pawn.RaceProps.IsMechanoid && pawn.kindDef != RM_DefOf.RM_Mech_Walker)
            {
                Log.ResetMessageCount();
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
                        __result = HealOrFollowOtherMechanoids(pawn);
                    }
                    return false;
                }
                else
                {
                    var lord = pawn.GetLord();
                    if (lord.ownedPawns.Any(x => x.kindDef == RM_DefOf.RM_Mech_Walker))
                    {
                        var firstCloseWalker = lord.ownedPawns.Where(x => x.kindDef == RM_DefOf.RM_Mech_Walker).OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                        var fightEnemiesJobGiver = new JobGiver_AIFightEnemiesNearToWalker();
                        JobGiver_AIFightEnemiesNearToWalker.walker = firstCloseWalker;
                        fightEnemiesJobGiver.ResolveReferences();
                        fightEnemiesJobGiver.targetAcquireRadius = 12f;
                        fightEnemiesJobGiver.targetKeepRadius = 12f;
                        var fightEnemiesJob = fightEnemiesJobGiver.TryGiveJob(pawn);
                        JobGiver_AIFightEnemiesNearToWalker.walker = null;
                        if (fightEnemiesJob != null)
                        {
                            //Log.Message(pawn + " - Found nearest target: " + fightEnemiesJob);
                            __result = fightEnemiesJob;
                            return false;
                        }
                        var nearestCell = JobGiver_WalkToPlayerBase.GetNearestCellToPlayerBase(firstCloseWalker, out var centerColony, out var firstBlockingBuilding);
                        //Log.Message(pawn + " - Found nearest cell: " + nearestCell + " - firstBlockingBuilding: " + firstBlockingBuilding);
                        if (firstBlockingBuilding != null && firstBlockingBuilding.Position.DistanceTo(pawn.Position) <= 10)
                        {
                            __result = MeleeAttackJob(firstBlockingBuilding);
                            if (__result != null)
                            {
                                //Log.Message(pawn + " - Should trash it: " + firstBlockingBuilding + " - " + __result);
                                return false;
                            }
                        }
                        if (firstCloseWalker.CurJobDef == JobDefOf.Wait)
                        {
                            //Log.Message(pawn + " - walker is waiting, doing usual stuff");
                            return true;
                        }
                        else
                        {
                            var followJob = TryGiveFollowJob(pawn, firstCloseWalker, 6);
                            if (followJob != null)
                            {
                                followJob.locomotionUrgency = LocomotionUrgency.Amble;
                                __result = followJob;
                                //Log.Message(pawn + " - Following Walker");
                                return false;
                            }
                            else
                            {
                                //Log.Message(pawn + " - Cannot follow walker, doing usual stuff");
                            }
                        }
                    }
                }
                //Log.Message(pawn + " is doing usual stuff");
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

        public static Job HealOrFollowOtherMechanoids(Pawn pawn)
        {
            var lord = pawn.GetLord();
            if (lord != null)
            {
                var otherPawns = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)
                    .Where(x => x.RaceProps.IsMechanoid && !x.Fogged() && !x.Dead && !x.Awake()).Except(pawn)
                    .OrderBy(x => x.Position.DistanceTo(pawn.Position)).ToList();
                foreach (var otherPawn in otherPawns)
                {
                    if (otherPawn.CanBeHealed() && pawn.CanReach(otherPawn, PathEndMode.Touch, Danger.None))
                    {
                        var job = JobMaker.MakeJob(RM_DefOf.RM_RepairMechanoid, otherPawn);
                        job.locomotionUrgency = LocomotionUrgency.Sprint;
                        return job;
                    }
                }
                if (lord.ownedBuildings != null)
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
                foreach (var otherPawn2 in otherPawns.InRandomOrder()) 
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
            return pawn.health.hediffSet.hediffs.Any(x => x is Hediff_Injury);
        }

        private static Job TryGiveFollowJob(Pawn pawn, Pawn followee, float radius)
        {
            if (!followee.Spawned || !pawn.CanReach(followee, PathEndMode.Touch, Danger.None))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.FollowClose, followee);
            job.expiryInterval = 140;
            job.checkOverrideOnExpire = true;
            job.followRadius = radius;
            return job;
        }
    }
}
