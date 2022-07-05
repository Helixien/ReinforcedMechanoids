using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ReinforcedMechanoids
{

    public class WorkGiver_DoMechanoidRepairing : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var mech in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)
                .Where(x => (x.GetComp<CompRepairable>()?.repairingAllowed ?? false) 
                    && pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Deadly)))
            {
                yield return mech;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return this.JobOnThing(pawn, t, forced) != null;
        }
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (thing is Pawn mech)
            {
                var comp = mech.GetComp<CompRepairable>();
                var chosen = comp.GetIngredientsForRepairing(pawn, mech, out bool fullRepairing);
                if (chosen != null)
                {
                    comp.currentRepairingMode = fullRepairing ? RepairingMode.Full : RepairingMode.Improvised;
                    var job = JobMaker.MakeJob(RM_DefOf.RM_RepairPlayerMechanoid);
                    job.targetA = mech;
                    job.targetC = mech;
                    job.targetQueueB = new List<LocalTargetInfo>(chosen.Count);
                    job.countQueue = new List<int>(chosen.Count);
                    for (var i = 0; i < chosen.Count; i++)
                    {
                        job.targetQueueB.Add(chosen[i].Thing);
                        job.countQueue.Add(chosen[i].Count);
                    }
                    return job;
                }
            }
            return null;
        }
    }
}

