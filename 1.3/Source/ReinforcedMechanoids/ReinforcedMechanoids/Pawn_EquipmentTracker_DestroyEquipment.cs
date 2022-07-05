using HarmonyLib;
using Verse;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "DestroyEquipment")]
    class Pawn_EquipmentTracker_DestroyEquipment
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

