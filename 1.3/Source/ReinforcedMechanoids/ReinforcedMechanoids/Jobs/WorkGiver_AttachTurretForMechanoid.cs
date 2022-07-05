using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using VFE.Mechanoids;

namespace ReinforcedMechanoids
{
    public class WorkGiver_AttachTurretForMechanoid : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is Pawn mech && CompMachine.cachedMachinesPawns.TryGetValue(mech, out var comp))
            {
                var compTurretAttachable = mech.GetComp<CompMachine_TurretAttachable>();
                if (compTurretAttachable == null || compTurretAttachable.turretToInstall == null)
                    return false;

                List<ThingDefCountClass> products = compTurretAttachable.turretToInstall.costList;
                foreach (ThingDefCountClass thingNeeded in products)
                {
                    if (!pawn.Map.itemAvailability.ThingsAvailableAnywhere(thingNeeded, pawn))
                    {
                        JobFailReason.Is("VFEMechNoResources".Translate());
                        return false;
                    }
                }
                return pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Deadly, ignoreOtherReservations: forced);
            }
            return false;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            List<ThingDefCountClass> products = t.TryGetComp<CompMachine_TurretAttachable>().turretToInstall.costList;
            List<Thing> toGrab = new List<Thing>();
            List<int> toGrabCount = new List<int>();
            foreach (ThingDefCountClass thingNeeded in products)
            {
                List<Thing> thingsOfThisType = RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, t.Position, new IntRange(thingNeeded.count, thingNeeded.count), (Thing thing) => thing.def == thingNeeded.thingDef);
                if (thingsOfThisType == null)
                {
                    return null;
                }
                toGrab.AddRange(thingsOfThisType);
                int totalCountNeeded = thingNeeded.count;
                foreach (Thing thingGrabbed in thingsOfThisType)
                {
                    if (thingGrabbed.stackCount >= totalCountNeeded)
                    {
                        toGrabCount.Add(totalCountNeeded);
                        totalCountNeeded = 0;
                    }
                    else
                    {
                        toGrabCount.Add(thingGrabbed.stackCount);
                        totalCountNeeded -= thingGrabbed.stackCount;
                    }
                }
            }
            Job job = JobMaker.MakeJob(RM_DefOf.RM_AttachTurretToMechanoid, t);
            job.targetQueueB = toGrab.Select((Thing f) => new LocalTargetInfo(f)).ToList();
            job.countQueue = toGrabCount.ToList();
            return job;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> workSet = new List<Thing>();
            foreach (CompMachineChargingStation compMachineChargingStation in CompMachineChargingStation.cachedChargingStations)
            {
                try
                {
                    if (compMachineChargingStation.parent.Map == pawn.Map)
                    {
                        workSet.Add(compMachineChargingStation.parent);
                    }
                }
                catch { }
            }
            return workSet.AsEnumerable();
        }
    }
}

