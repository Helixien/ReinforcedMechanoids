using AnimalBehaviours;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ReinforcedMechanoids
{
    public class CompProperties_TurretAttachable : CompProperties
    {
        public CompProperties_TurretAttachable()
        {
            this.compClass = typeof(CompMachine_TurretAttachable);
        }
    }
    public class CompMachine_TurretAttachable : ThingComp, PawnGizmoProvider
    {
        public ThingDef turretToInstall;

        public ThingDef turretAttached;
        public IEnumerable<Gizmo> GetGizmos()
        {
            if (this.parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = "VFEMechAttachTurret".Translate(),
                    defaultDesc = "VFEMechAttachTurretDesc".Translate(),
                    icon = turretAttached != null ? turretAttached.building.turretGunDef.uiIcon : turretToInstall != null
                    ? turretToInstall.building.turretGunDef.uiIcon : null,
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs.Where(t =>
                                 t.building != null
                                 && t.building.turretGunDef != null
                                 && t.costList != null
                                 && t.GetCompProperties<CompProperties_Mannable>() == null
                                 && t.size.x <= 3
                                 && t.size.z <= 3
                                 && t.IsResearchFinished
                        ))
                        {
                            FloatMenuOption opt = new FloatMenuOption(thing.label, delegate
                            {
                                turretToInstall = thing;
                            }, thing.building.turretGunDef);
                            options.Add(opt);
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }
        }

        public void AttachTurret()
        {
            if (turretAttached != null)
            {
                foreach (ThingDefCountClass stack in turretAttached.costList)
                {
                    Thing thing = ThingMaker.MakeThing(stack.thingDef);
                    thing.stackCount = stack.count;
                    GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
                }
                ((Pawn)parent).equipment.DestroyAllEquipment();
            }
            turretAttached = turretToInstall;
            Thing turretThing = ThingMaker.MakeThing(turretAttached.building.turretGunDef);
            ((Pawn)parent).equipment.AddEquipment((ThingWithComps)turretThing);
            this.turretToInstall = null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref turretToInstall, "turretToInstall");
            Scribe_Defs.Look(ref turretAttached, "turretAttached");
        }
    }
}

