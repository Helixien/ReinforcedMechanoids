using System.Linq;
using System;
using Verse;
using RimWorld;

namespace ReinforcedMechanoids
{
    public class Verb_MeleeAttackDamageBehemoth : Verb_MeleeAttackDamage
    {
        public override bool Available()
        {
            return HarmonyPatches.GetNonMissingBodyPart(CasterPawn, RM_DefOf.RM_BehemothShield) != null && base.Available();
        }
        public override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            if (target.Thing is Pawn victim)
            {
                if (victim.health.hediffSet.GetFirstHediffOfDef(RM_DefOf.RM_BehemothAttack) is null)
                {
                    victim.health.AddHediff(HediffMaker.MakeHediff(RM_DefOf.RM_BehemothAttack, victim));
                }
            }
            return base.ApplyMeleeDamageToTarget(target);
        }
    }
}
