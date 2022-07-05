using AnimalBehaviours;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ReinforcedMechanoids
{
    public class MechanoidHackingPropertiesDef : Def
    {
        public ThingDef hackingBase;
        public List<PawnKindDef> hackableMechanoids = new List<PawnKindDef>();
        public List<PawnKindDef> supportMechanoids = new List<PawnKindDef>();
        public List<PawnKindDef> meleeMechanoids = new List<PawnKindDef>();
    }

    public class CompProperties_Repairable : CompProperties
    {
        public CompProperties_Repairable()
        {
            this.compClass = typeof(CompRepairable);
        }
    }

    public enum RepairingMode
    {
        None, Full, Improvised
    }
    public class CompRepairable : ThingComp, PawnGizmoProvider
    {
        public bool repairingAllowed;
        public RepairingMode currentRepairingMode;
        public IEnumerable<Gizmo> GetGizmos()
        {
            if (this.parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "RM.AllowRepairing".Translate(),
                    defaultDesc = "RM.AllowRepairingDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/RepairMechanoid"),
                    toggleAction = () => repairingAllowed = !repairingAllowed,
                    isActive = () => repairingAllowed
                };
            }
        }

        public List<IngredientCount> IngredientsForFullRepairing()
        {
            var thingCounts = new Dictionary<ThingDef, int>();
            var pawn = this.parent as Pawn;
            var baseHealthScale = pawn.def.race.baseHealthScale;
            var currentHealth = pawn.health.summaryHealth.SummaryHealthPercent;
            int plasteelCount = Mathf.RoundToInt((8f * baseHealthScale) * (1f - currentHealth));
            int spacerComponentCount = Mathf.RoundToInt(baseHealthScale * (1f - currentHealth));
            thingCounts.Add(ThingDefOf.Plasteel, plasteelCount);
            thingCounts.Add(ThingDefOf.ComponentSpacer, spacerComponentCount);
            var ingredientCountList = new List<IngredientCount>();
            foreach (var data in thingCounts)
            {
                ingredientCountList.Add(new ThingDefCountClass(data.Key, data.Value).ToIngredientCount());
            }
            return ingredientCountList;
        }

        public List<IngredientCount> IngredientsForImprovisedRepairing()
        {
            var thingCounts = new Dictionary<ThingDef, int>();
            var pawn = this.parent as Pawn;
            var baseHealthScale = pawn.def.race.baseHealthScale;
            var currentHealth = pawn.health.summaryHealth.SummaryHealthPercent;
            int steelCount = Mathf.RoundToInt((15f * baseHealthScale) * (1f - currentHealth));
            int industrialComponentCount = Mathf.RoundToInt((2f * baseHealthScale) * (1f - currentHealth));
            thingCounts.Add(ThingDefOf.Steel, steelCount);
            thingCounts.Add(ThingDefOf.ComponentIndustrial, industrialComponentCount);
            var ingredientCountList = new List<IngredientCount>();
            foreach (var data in thingCounts)
            {
                ingredientCountList.Add(new ThingDefCountClass(data.Key, data.Value).ToIngredientCount());
            }
            return ingredientCountList;
        }

        public List<ThingCount> GetIngredientsForRepairing(Pawn worker, Pawn mech, out bool fullRepairing)
        {
            fullRepairing = false;
            var chosen = new List<ThingCount>();
            if (!RepairingUtility.TryFindBestFixedIngredients(IngredientsForFullRepairing(), worker, mech, chosen)
                || chosen.Any(x => !worker.CanReserveAndReach(x.Thing, PathEndMode.ClosestTouch, Danger.Deadly)))
            {
                chosen.Clear();
                if (!RepairingUtility.TryFindBestFixedIngredients(IngredientsForImprovisedRepairing(), worker, mech, chosen)
                || chosen.Any(x => !worker.CanReserveAndReach(x.Thing, PathEndMode.ClosestTouch, Danger.Deadly)))
                {
                    return null;
                }
            }
            else
            {
                fullRepairing = true;
            }
            return chosen;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref repairingAllowed, "repairingAllowed");
            Scribe_Values.Look(ref currentRepairingMode, "currentRepairingMode");
        }
    }
}

