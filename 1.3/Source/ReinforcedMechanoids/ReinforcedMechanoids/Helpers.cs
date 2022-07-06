using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using VFEMech;

namespace ReinforcedMechanoids
{
    public static class Helpers
    {
        public static bool CanBeHacked(this PawnKindDef pawnKindDef, ThingDef byStation = null)
        {
            return GetFirstMatchingHackingDef(pawnKindDef, byStation) != null;
        }

        public static MechanoidHackingPropertiesDef GetFirstMatchingHackingDef(this PawnKindDef pawnKindDef, ThingDef byStation)
        {
            foreach (var def in DefDatabase<MechanoidHackingPropertiesDef>.AllDefs)
            {
                if (byStation is null || def.hackingBase == byStation)
                {
                    if (def.hackableMechanoids.Contains(pawnKindDef) || def.supportMechanoids.Contains(pawnKindDef)
                        || def.meleeMechanoids.Contains(pawnKindDef))
                    {
                        return def;
                    }
                }
            }
            return null;
        }

        public static bool IsMechanoidHacked(this Pawn pawn)
        {
            if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.kindDef.CanBeHacked())
            {
                return true;
            }
            return false;
        }

        public static Thing GetAvailableMechanoidStation(Pawn pawn, Pawn targetPawn, bool checkForPower = false, bool forHacking = false)
        {
            return GenClosest.ClosestThingReachable(targetPawn.Position, targetPawn.MapHeld, 
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, 
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, delegate (Thing b)
            {
                var platform = b.TryGetComp<CompMechanoidStation>();
                if (platform != null 
                    && ((platform.myPawn is null || platform.myPawn == targetPawn) 
                    && (!forHacking || platform.mechanoidToHack == targetPawn)
                    && (!checkForPower || (platform.compPower?.PowerOn ?? false))))
                {
                    return true;
                }
                return false;
            });
        }
    }
}

