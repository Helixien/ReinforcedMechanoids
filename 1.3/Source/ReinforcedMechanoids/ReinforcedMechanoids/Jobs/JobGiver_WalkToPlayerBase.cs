using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace ReinforcedMechanoids
{
    public class JobGiver_WalkToPlayerBase : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer)
            {
                var otherPawns = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)
                    .Where(x => x.RaceProps.IsMechanoid && !x.Fogged() && !x.Dead && x.Awake()).Except(pawn)
                    .OrderBy(x => x.Position.DistanceTo(pawn.Position)).ToList();
                if (otherPawns.Count(x => x.IsFighting()) >= otherPawns.Count / 3f)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                    job.expiryInterval = 180;
                    return job;
                }
                var nearestCell = GetNearestCellToPlayerBase(pawn, out var centerColony, out var firstBlockingBuilding);
                if (nearestCell == pawn.Position)
                {
                    var job = JobMaker.MakeJob(JobDefOf.Wait);
                    job.expiryInterval = 180;
                    return job;
                }
                else if (nearestCell.IsValid)
                {
                    var job = JobMaker.MakeJob(JobDefOf.Goto, nearestCell);
                    job.expiryInterval = 180;
                    return job;
                }
            }
            return null;
        }
        
        public static Dictionary<Pawn, CachedValue> cachedValuesPawn = new Dictionary<Pawn, CachedValue>();
        public static IntVec3 GetNearestCellToPlayerBase(Pawn pawn, out IntVec3 centerColony, out Building firstBlockingBuilding)
        {
            centerColony = FindCenterColony(pawn.Map);
            if (cachedValuesPawn.TryGetValue(pawn, out var cachedValue))
            {
                if (Find.TickManager.TicksGame < cachedValue.lastCheckTick + 180)
                {
                    firstBlockingBuilding = cachedValue.firstBlockingBuilding;
                    return cachedValue.value;
                }
            }
            firstBlockingBuilding = null;
            var path = pawn.Map.pathFinder.FindPath(pawn.Position, centerColony, TraverseMode.PassAllDestroyableThingsNotWater);
            IntVec3 prevCell = pawn.Position;
            var pathNodes = path.NodesReversed.ListFullCopy();
            pathNodes.Reverse();
            pawn.Map.pawnPathPool.paths.Clear();
            foreach (var cell in pathNodes)
            {
                firstBlockingBuilding = cell.GetEdifice(pawn.Map);
                if (cell.Roofed(pawn.Map) 
                    || firstBlockingBuilding != null 
                    || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
                {
                    if (prevCell != pawn.Position)
                    {
                        cachedValuesPawn[pawn] = new CachedValue
                        {
                            value = prevCell,
                            firstBlockingBuilding = firstBlockingBuilding,
                            lastCheckTick = Find.TickManager.TicksGame
                        };
                        return prevCell;
                    }
                    else
                    {
                        cachedValuesPawn[pawn] = new CachedValue
                        {
                            value = IntVec3.Invalid,
                            firstBlockingBuilding = firstBlockingBuilding,
                            lastCheckTick = Find.TickManager.TicksGame
                        };
                        return IntVec3.Invalid;
                    }
                }
                prevCell = cell;
            }
            cachedValuesPawn[pawn] = new CachedValue
            {
                value = centerColony,
                firstBlockingBuilding = firstBlockingBuilding,
                lastCheckTick = Find.TickManager.TicksGame
            };
            return centerColony;
        }

        public class CachedValue
        {
            public IntVec3 value;
            public int lastCheckTick;
            public Building firstBlockingBuilding;
        }
        public static Dictionary<Map, CachedValue> cachedValuesCenterColony = new Dictionary<Map, CachedValue>();
        public static IntVec3 FindCenterColony(Map map)
        {
            if (cachedValuesCenterColony.TryGetValue(map, out var cachedValue))
            {
                if (Find.TickManager.TicksGame < cachedValue.lastCheckTick + GenDate.TicksPerHour)
                {
                    return cachedValue.value;
                }
            }
            var colonyThings = map.listerThings.AllThings.Where(x => x.Faction == Faction.OfPlayer).Select(x => x.Position);
            if (colonyThings.Any())
            {
                var x_Averages = colonyThings.OrderBy(x => x.x);
                var x_average = x_Averages.ElementAt(x_Averages.Count() / 2).x;
                var z_Averages = colonyThings.OrderBy(x => x.z);
                var z_average = z_Averages.ElementAt(z_Averages.Count() / 2).z;
                var middleCell = new IntVec3(x_average, 0, z_average);
                cachedValuesCenterColony[map] = new CachedValue
                {
                    value = middleCell,
                    lastCheckTick = Find.TickManager.TicksGame
                };
                return middleCell;
            }
            cachedValuesCenterColony[map] = new CachedValue
            {
                value = map.Center,
                lastCheckTick = Find.TickManager.TicksGame
            };
            return map.Center;
        }
    }
}
