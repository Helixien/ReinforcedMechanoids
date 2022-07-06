using HarmonyLib;
using Verse;
using VFE.Mechanoids;
using VFE.Mechanoids.AI.JobGivers;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(JobGiver_ReturnToStationIdle), "TryGiveJob")]
    public static class JobGiver_ReturnToStationIdle_TryGiveJob
    {
        public static bool Prefix(Pawn pawn)
        {
            return JobGiver_Recharge_TryGiveJob.TrySearchForStation(pawn);
        }
    }

    [HarmonyPatch(typeof(JobGiver_Recharge), "TryGiveJob")]
    public static class JobGiver_Recharge_TryGiveJob
    {
        public static bool Prefix(Pawn pawn)
        {
            return TrySearchForStation(pawn);
        }
        public static bool TrySearchForStation(Pawn pawn)
        {
            if (pawn.IsMechanoidHacked())
            {
                var comp = pawn.TryGetComp<CompMachine>();
                if (comp.myBuilding is null)
                {
                    var building = Helpers.GetAvailableMechanoidStation(pawn, pawn);
                    if (building != null)
                    {
                        comp.myBuilding = building as Building;
                        var compStation = building.TryGetComp<CompMechanoidStation>();
                        compStation.myPawn = pawn;
                        compStation.mechanoidToHack = pawn;
                    }
                    else
                    {
                        Log.Message("Didn't find station, return false");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}

