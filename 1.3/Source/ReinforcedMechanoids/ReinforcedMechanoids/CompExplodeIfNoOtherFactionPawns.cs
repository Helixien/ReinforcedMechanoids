using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ReinforcedMechanoids
{
    public class CompProperties_ExplodeIfNoOtherFactionPawns : CompProperties
    {
        public float radius;
        public CompProperties_ExplodeIfNoOtherFactionPawns()
        {
            this.compClass = typeof(CompExplodeIfNoOtherFactionPawns);
        }
    }

    [StaticConstructorOnStartup]
    public class CompExplodeIfNoOtherFactionPawns : ThingComp
    {
        public CompProperties_ExplodeIfNoOtherFactionPawns Props => base.props as CompProperties_ExplodeIfNoOtherFactionPawns;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent is Pawn pawn)
            {
                if (pawn.equipment is null)
                {
                    pawn.equipment = new Pawn_EquipmentTracker(pawn);
                }
                if (pawn.apparel is null)
                {
                    pawn.apparel = new Pawn_ApparelTracker(pawn);
                }
            }
            this.TryExplode();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                TryExplode();
            }
        }
        public void TryExplode()
        {
            var pawn = this.parent as Pawn;
            if (pawn.Map != null && pawn.Faction != null)
            {
                if (!pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Where(x => x.kindDef != pawn.kindDef).Any())
                {
                    GenExplosion.DoExplosion(pawn.Position, pawn.Map, Props.radius, DamageDefOf.Bomb, pawn);
                    if (!pawn.Dead)
                    {
                        pawn.Kill(null);
                    }
                }
            }
        }

    }
}
