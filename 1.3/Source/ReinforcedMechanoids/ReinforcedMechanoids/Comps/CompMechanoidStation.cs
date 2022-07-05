using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using VFE.Mechanoids;

namespace ReinforcedMechanoids
{
    public class CompMechanoidStation : CompMachineChargingStation
    {
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
                yield return new Command_Action
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
                                this.parent.Map.designationManager.AddDesignation(new Designation(x, RM_DefOf.RM_HackMechanoid));
                            }
                        }, delegate (LocalTargetInfo t)
                        {
                            GenDraw.DrawTargetHighlight(t);
                        }, (LocalTargetInfo t) => true);
                    }
                };
            }
        }

    }
}

