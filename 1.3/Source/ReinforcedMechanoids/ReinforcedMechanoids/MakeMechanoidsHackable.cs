using AnimalBehaviours;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;
using VFE.Mechanoids;

namespace ReinforcedMechanoids
{
    [StaticConstructorOnStartup]
    public static class MakeMechanoidsHackable
    {
        static MakeMechanoidsHackable()
        {
            foreach (var pawn in DefDatabase<PawnKindDef>.AllDefs)
            {
                var extension = pawn.GetFirstMatchingHackingDef(null);
                if (extension != null)
                {
                    var draftableCompProps = pawn.race.GetCompProperties<CompProperties_Draftable>();
                    if (draftableCompProps is null)
                    {
                        pawn.race.comps.Add(new CompProperties_Draftable());
                    }
                    var compMachineProps = pawn.race.GetCompProperties<CompProperties_Machine>();
                    if (compMachineProps is null)
                    {
                        pawn.race.comps.Add(new CompProperties_Machine
                        {
                            violent = true,
                            canPickupWeapons = !extension.meleeMechanoids.Contains(pawn),
                            hoursActive = 100 * pawn.race.race.baseBodySize,
                            compClass = typeof(CompMachine)
                        });
                    }

                    if (extension.supportMechanoids.Contains(pawn))
                    {
                        var compTurretAttachable = pawn.race.GetCompProperties<CompProperties_TurretAttachable>();
                        if (compTurretAttachable is null)
                        {
                            pawn.race.comps.Add(new CompProperties_TurretAttachable());
                        }
                    }

                    var compRepairableProps = pawn.race.GetCompProperties<CompProperties_Repairable>();
                    if (compRepairableProps is null)
                    {
                        pawn.race.comps.Add(new CompProperties_Repairable());
                    }
                    var modExtension = pawn.race.GetModExtension<MechanoidExtension>();
                    if (modExtension is null)
                    {
                        if (pawn.race.modExtensions is null)
                        {
                            pawn.race.modExtensions = new List<DefModExtension>();
                        }
                        pawn.race.modExtensions.Add(new MechanoidExtension
                        {
                            isCaravanRiddable = true,
                            hasPowerNeedWhenHacked = true,
                        });
                    }
                }
            }
        }
    }
}

