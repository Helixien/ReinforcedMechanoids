using HarmonyLib;
using RimWorld;
using System.Diagnostics;
using UnityEngine;
using Verse;
using VFEMech;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(StatWorker), "IsDisabledFor")]
    static class StatWorker_IsDisabledFor
    {
        static bool Prefix(Thing thing, ref bool __result)
        {
            if (thing is Pawn && ((Pawn)thing).RaceProps.IsMechanoid)
            {
                Pawn pawn = (Pawn)thing;
                if (pawn.Faction == Faction.OfPlayer)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}

