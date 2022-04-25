using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.XR;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ReinforcedMechanoids
{
    public class JobDriver_RepairMechanoid : JobDriver
    {
        public int TotalWorkTick = 300;
        private int workDone;
        protected Pawn ToRepair => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            base.Map.reservationManager.ReleaseAllForTarget(ToRepair);
            return pawn.Reserve(ToRepair, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedOrNull(TargetIndex.A)
                .FailOn(() => !pawn.CanReach(job.GetTarget(TargetIndex.A), PathEndMode.Touch, Danger.None))
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            
            yield return new Toil
            {
                initAction = delegate
                {
                    PawnUtility.ForceWait(ToRepair, TotalWorkTick, this.pawn);
                },
                tickAction = delegate
                {
                    if (this.pawn.IsHashIntervalTick(4))
                    {
                        workDone++;
                        if (workDone >= TotalWorkTick)
                        {
                            this.EndJobWith(JobCondition.Succeeded);
                        }
                        else if (ToRepair.health.hediffSet.hediffs.OfType<Hediff_Injury>().TryRandomElement(out var injury))
                        {
                            injury.Heal(0.09f);
                        }
                        else
                        {
                            this.EndJobWith(JobCondition.Succeeded);
                        }
                    }
                },
            	defaultCompleteMode = ToilCompleteMode.Never
            }.WithEffect(() => EffecterDefOf.ConstructMetal, TargetIndex.A);
		}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workDone, "workDone");
        }
    }
}
