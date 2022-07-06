using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using VFE.Mechanoids;

namespace ReinforcedMechanoids
{
    public class CompMechanoidStation : CompMachineChargingStation
    {
        public Pawn mechanoidToHack;
        public CompPowerTrader compPower;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compPower = this.parent.GetComp<CompPowerTrader>();
        }
        public override void SpawnMyPawn()
        {
            wantsRespawn = false;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (this.parent.Faction == Faction.OfPlayer)
            {
                var command = new Command_Action
                {
                    defaultLabel = "RM.SelectMechanoid".Translate(),
                    defaultDesc = "RM.SelectMechanoidDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/SelectMechanoid"),
                    action = delegate
                    {
                        Find.Targeter.BeginTargeting(new TargetingParameters
                        {
                            canTargetItems = true,
                            mapObjectTargetsMustBeAutoAttackable = false,
                            validator = delegate (TargetInfo target)
                            {
                                return target.Thing is Corpse corspe && (corspe.InnerPawn?.kindDef.CanBeHacked(this.parent.def) ?? false);
                            },
                        }, delegate (LocalTargetInfo x)
                        {
                            if (x.Thing != null)
                            {
                                var designation = this.parent.Map.designationManager.DesignationOn(x.Thing);
                                if (designation != null)
                                {
                                    this.parent.Map.designationManager.RemoveDesignation(designation);
                                }
                                this.parent.Map.designationManager.AddDesignation(new Designation(x, RM_DefOf.RM_HackMechanoid));
                                mechanoidToHack = x.Thing is Corpse corpse ? corpse.InnerPawn : null;
                            }
                        }, delegate (LocalTargetInfo t)
                        {
                            GenDraw.DrawTargetHighlight(t);
                        }, (LocalTargetInfo t) => true);
                    }
                };
                if (compPower != null && !compPower.PowerOn)
                {
                    command.Disable("NoPower".Translate());
                }
                yield return command;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref mechanoidToHack, "mechanoidToHack");
        }
    }
}

