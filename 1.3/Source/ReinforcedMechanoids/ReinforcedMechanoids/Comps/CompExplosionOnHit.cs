using RimWorld;
using Verse;

namespace ReinforcedMechanoids
{
    public class CompProperties_ExplosionOnHit : CompProperties_Explosive
    {
        public int cooldownTicks = -1;
        public CompProperties_ExplosionOnHit()
        {
            this.compClass = typeof(CompExplosionOnHit);
        }
    }
    public class CompExplosionOnHit : ThingComp
    {
        public int lastExplosionTicks;
        public CompProperties_ExplosionOnHit Props => base.props as CompProperties_ExplosionOnHit;
        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);
            if (lastExplosionTicks > 0 && lastExplosionTicks + Props.cooldownTicks <= Find.TickManager.TicksGame)
            {
                lastExplosionTicks = Find.TickManager.TicksGame;
                Detonate(parent.Map);
            }
        }

        protected void Detonate(Map map, bool ignoreUnspawned = false)
        {
            if (!ignoreUnspawned && !parent.SpawnedOrAnyParentSpawned)
            {
                return;
            }
            CompProperties_Explosive compProperties_Explosive = Props;
            if (compProperties_Explosive.explosiveExpandPerFuel > 0f && parent.GetComp<CompRefuelable>() != null)
            {
                parent.GetComp<CompRefuelable>().ConsumeFuel(parent.GetComp<CompRefuelable>().Fuel);
            }
            var radius = Props.explosiveRadius;
            if (compProperties_Explosive.destroyThingOnExplosionSize <= radius && !parent.Destroyed)
            {
                parent.Kill();
            }
            if (map == null)
            {
                Log.Warning("Tried to detonate CompExplosive in a null map.");
                return;
            }
            if (compProperties_Explosive.explosionEffect != null)
            {
                Effecter effecter = compProperties_Explosive.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(parent.PositionHeld, map), new TargetInfo(parent.PositionHeld, map));
                effecter.Cleanup();
            }
            GenExplosion.DoExplosion(instigator: this.parent, center: parent.PositionHeld, map: map, radius: radius, damType: compProperties_Explosive.explosiveDamageType, damAmount: compProperties_Explosive.damageAmountBase, armorPenetration: compProperties_Explosive.armorPenetrationBase, explosionSound: compProperties_Explosive.explosionSound, weapon: null, projectile: null, intendedTarget: null, postExplosionSpawnThingDef: compProperties_Explosive.postExplosionSpawnThingDef, postExplosionSpawnChance: compProperties_Explosive.postExplosionSpawnChance, postExplosionSpawnThingCount: compProperties_Explosive.postExplosionSpawnThingCount, applyDamageToExplosionCellsNeighbors: compProperties_Explosive.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef: compProperties_Explosive.preExplosionSpawnThingDef, preExplosionSpawnChance: compProperties_Explosive.preExplosionSpawnChance, preExplosionSpawnThingCount: compProperties_Explosive.preExplosionSpawnThingCount, chanceToStartFire: compProperties_Explosive.chanceToStartFire, damageFalloff: compProperties_Explosive.damageFalloff, direction: null);
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastExplosionTicks, "lastExplosionTicks");
        }
    }
}

