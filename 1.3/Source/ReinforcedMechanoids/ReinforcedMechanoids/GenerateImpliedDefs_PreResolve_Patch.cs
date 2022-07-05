using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using VFEMech;

namespace ReinforcedMechanoids
{
    [HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResetStaticData")]
    public static class WorkGiver_ConstructDeliverResources_ResetStaticData_Patch
    {
        public static void Postfix()
        {
            CreateMechComponents();
        }
        public static void CreateMechComponents()
        {
            foreach (var pawn in DefDatabase<PawnKindDef>.AllDefs)
            {
                if (pawn.CanBeHacked())
                {
                    if (!pawn.race.race.thinkTreeMain.thinkRoot.subNodes.Any(x => x is ThinkNode_Subtree subtree
                         && subtree.treeDef == RM_DefOf.RM_MechanoidHacked_Behaviour))
                    {
                        if (pawn.race.race.thinkTreeConstant != null)
                        {
                            var node = pawn.race.race.thinkTreeConstant.thinkRoot.subNodes
                                .Find(x => x is ThinkNode_ConditionalCanDoConstantThinkTreeJobNow);
                            if (node != null)
                            {
                                if (!node.subNodes.Exists(x => x is ThinkNode_Subtree subtree && subtree.treeDef == RM_DefOf.JoinAutoJoinableCaravan))
                                {
                                    node.subNodes.Add(new ThinkNode_Subtree
                                    {
                                        treeDef = RM_DefOf.JoinAutoJoinableCaravan,
                                    });
                                }

                                if (!node.subNodes.Exists(x => x is ThinkNode_Subtree subtree && subtree.treeDef == RM_DefOf.LordDutyConstant))
                                {
                                    node.subNodes.Add(new ThinkNode_Subtree
                                    {
                                        treeDef = RM_DefOf.LordDutyConstant,
                                    });
                                }
                            }
                        }
                        else
                        {
                            pawn.race.race.thinkTreeConstant = RM_DefOf.VFE_Mechanoids_Machine_RiddableConstant;
                        }

                        var index = pawn.race.race.thinkTreeMain.thinkRoot.subNodes.FindIndex(x => x is ThinkNode_Subtree subtree
                            && subtree.treeDef == RM_DefOf.Downed);
                        if (index >= 0)
                        {
                            var toAdd = new ThinkNode_Subtree
                            {
                                treeDef = RM_DefOf.RM_MechanoidHacked_Behaviour,
                            };
                            if (index + 1 < pawn.race.race.thinkTreeMain.thinkRoot.subNodes.Count)
                            {
                                pawn.race.race.thinkTreeMain.thinkRoot.subNodes.Insert(index + 1, toAdd);
                            }
                            else
                            {
                                pawn.race.race.thinkTreeMain.thinkRoot.subNodes.Add(toAdd);
                            }
                        }
                    }
                }
            }
        }
    }
}

