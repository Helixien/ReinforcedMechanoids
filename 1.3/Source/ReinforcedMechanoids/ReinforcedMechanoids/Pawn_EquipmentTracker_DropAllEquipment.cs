using HarmonyLib;
using Verse;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "Notify_PawnSpawned")]
    class Pawn_EquipmentTracker_DropAllEquipment
    {
        static bool Prefix(Pawn_EquipmentTracker __instance)
        {
            if (__instance.pawn.RaceProps.IsMechanoid && !__instance.pawn.Dead)
            {
                return false;
            }
            return true;
        }
    }
}

