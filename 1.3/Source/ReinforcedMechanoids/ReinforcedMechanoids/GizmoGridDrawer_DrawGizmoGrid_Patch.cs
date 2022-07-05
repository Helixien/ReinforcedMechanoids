using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(GizmoGridDrawer), "DrawGizmoGrid")]
    public static class GizmoGridDrawer_DrawGizmoGrid_Patch
    {
        static void Prefix(ref IEnumerable<Gizmo> gizmos)
        {
            gizmos = gizmos.Where(x => x != null);
        }
    }
}

