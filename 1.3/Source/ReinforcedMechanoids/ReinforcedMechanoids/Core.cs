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
            new Harmony("ReinforcedMechanoids.Mod").PatchAll();
        }
    }

    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class JobGiver_AIFightEnemy_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
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
            return true;
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
                    var job = TryGiveFollowJob(pawn, otherPawn2);
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

        private static Job TryGiveFollowJob(Pawn pawn, Pawn followee)
        {
            if (followee == null)
            {
                return null;
            }
            if (!followee.Spawned || !pawn.CanReach(followee, PathEndMode.OnCell, Danger.Deadly))
            {
                return null;
            }
            float radius = 25;
            if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, followee, radius))
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
