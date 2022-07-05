using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(ITab_Pawn_Character))]
    [HarmonyPatch("IsVisible", MethodType.Getter)]
    public static class ITab_Pawn_Character_IsVisible_Patch
    {
        public delegate Pawn PawnToShowInfoAbout(ITab_Pawn_Character __instance);
        public static readonly PawnToShowInfoAbout pawnToShowInfoAbout = AccessTools.MethodDelegate<PawnToShowInfoAbout>
            (AccessTools.Method(typeof(ITab_Pawn_Character), "get_PawnToShowInfoAbout"));
        public static void Postfix(ITab_Pawn_Character __instance, ref bool __result)
        {
            Pawn pawn = pawnToShowInfoAbout(__instance);
            if (pawn.RaceProps.IsMechanoid)
            {
                __result = false;
            }
        }
    }
}

