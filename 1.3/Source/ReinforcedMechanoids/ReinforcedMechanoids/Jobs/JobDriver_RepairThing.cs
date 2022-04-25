using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ReinforcedMechanoids
{

    public class JobDriver_RepairThing : JobDriver
    {
        protected float ticksToNextRepair;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil repair = new Toil();
            repair.initAction = delegate
            {
                ticksToNextRepair = 80f;
            };
            repair.tickAction = delegate
            {
                Pawn actor = repair.actor;
                actor.rotationTracker.FaceTarget(actor.CurJob.GetTarget(TargetIndex.A));
                float num = 1f;
                ticksToNextRepair -= num;
                if (ticksToNextRepair <= 0f)
                {
                    ticksToNextRepair += 35f;
                    base.TargetThingA.HitPoints++;
                    base.TargetThingA.HitPoints = Mathf.Min(base.TargetThingA.HitPoints, base.TargetThingA.MaxHitPoints);
                    base.Map.listerBuildingsRepairable.Notify_BuildingRepaired((Building)base.TargetThingA);
                    if (base.TargetThingA.HitPoints == base.TargetThingA.MaxHitPoints)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            repair.handlingFacing = true;
            yield return repair;
        }
    }
}
