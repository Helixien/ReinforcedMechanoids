using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ReinforcedMechanoids
{
    public class JobDriver_HackMechanoidCorpseAtMechanoidStation : JobDriver
    {
        public float hackingPeriod;

        private bool forbiddenInitially;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed))
            {
                if (pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed))
                {
                    return pawn.Reserve(job.GetTarget(TargetIndex.C), job, 1, -1, null, errorOnFailed);
                }
            }
            return false;
        }
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            if (base.TargetThingA != null)
            {
                forbiddenInitially = base.TargetThingA.IsForbidden(pawn);
            }
            else
            {
                forbiddenInitially = false;
            }
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.C);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.B);
            this.FailOn(delegate
            {
                var comp = TargetC.Thing.TryGetComp<CompPowerTrader>();
                if (comp != null)
                {
                    return comp.PowerOn is false;
                }
                return false;
            });
            if (!forbiddenInitially)
            {
                this.FailOnForbidden(TargetIndex.A);
            }

            if (TargetA.Cell != TargetC.Cell)
            {
                Toil toilGoto = null;
                toilGoto = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A)
                    .FailOn((Func<bool>)delegate
                    {
                        Pawn actor = toilGoto.actor;
                        Job curJob = actor.jobs.curJob;
                        if (curJob.haulMode == HaulMode.ToCellStorage)
                        {
                            Thing thing = curJob.GetTarget(TargetIndex.A).Thing;
                            if (!actor.jobs.curJob.GetTarget(TargetIndex.B).Cell.IsValidStorageFor(base.Map, thing))
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                yield return toilGoto;
                yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
                Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B, PathEndMode.Touch);
                yield return carryToCell;
                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, storageMode: true);
            }

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var hack = new Toil
            {
                initAction = delegate
                {
                    hackingPeriod = GetInitialHackingPeriod();
                },
                tickAction = delegate
                {
                    hackingPeriod--;
                    if (hackingPeriod <= 0)
                    {
                        var corpse = TargetA.Thing as Corpse;
                        var mech = corpse.InnerPawn;
                        ResurrectionUtility.Resurrect(mech);
                        var eq = mech.equipment.Primary;
                        if (eq != null)
                        {
                            mech.equipment.Remove(eq);
                        }
                        PawnComponentsUtility_AddAndRemoveDynamicComponents.RefillPawnComponents(mech);
                        mech.SetFaction(Faction.OfPlayer);
                        if (eq != null)
                        {
                            mech.equipment.AddEquipment(eq);
                        }
                        var comp = TargetC.Thing.TryGetComp<CompMechanoidStation>();
                        comp.myPawn = mech;
                        comp.mechanoidToHack = null;
                        ReadyForNextToil();
                    }
                }
            };
            hack.FailOn(x => !TargetC.Thing.TryGetComp<CompPowerTrader>().PowerOn);
            hack.defaultCompleteMode = ToilCompleteMode.Never;
            hack.WithEffect(() => RM_DefOf.RM_Hacking, TargetIndex.A);
            hack.activeSkill = () => SkillDefOf.Intellectual;
            hack.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            hack.WithProgressBar(TargetIndex.A, () => (1f - (hackingPeriod / GetInitialHackingPeriod())));
            yield return hack;
        }

        private float GetInitialHackingPeriod()
        {
            return 10000f - (pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * 250f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hackingPeriod, "hackingPeriod");
            Scribe_Values.Look(ref forbiddenInitially, "forbiddenInitially", defaultValue: false);
        }
    }
}

