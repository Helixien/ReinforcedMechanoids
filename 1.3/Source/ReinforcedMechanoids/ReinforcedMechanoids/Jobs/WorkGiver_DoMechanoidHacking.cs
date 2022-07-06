using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ReinforcedMechanoids
{
    public class WorkGiver_DoMechanoidHacking : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Designation item in pawn.Map.designationManager.SpawnedDesignationsOfDef(RM_DefOf.RM_HackMechanoid))
            {
                yield return item.target.Thing;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return this.JobOnThing(pawn, t, forced) != null;
        }
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (thing is Corpse corpse && pawn.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly))
            {
                var station = Helpers.GetAvailableMechanoidStation(pawn, corpse.InnerPawn, checkForPower: true, forHacking: true) as Building;
                if (station != null && pawn.CanReserveAndReach(station, PathEndMode.Touch, Danger.Deadly) 
                    && (station.Position != corpse.Position || station.TryGetComp<CompPowerTrader>().PowerOn))
                {
                    Job job = JobMaker.MakeJob(RM_DefOf.RM_HackMechanoidCorpseAtMechanoidStation, corpse, station.Position, station);
                    job.count = 1;
                    return job;
                }
            }
            return null;
        }
    }
}

