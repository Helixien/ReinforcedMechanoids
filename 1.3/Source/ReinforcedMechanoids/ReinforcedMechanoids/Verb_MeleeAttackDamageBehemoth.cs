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
            return Core.GetNonMissingBodyPart(CasterPawn, RM_DefOf.RM_BehemothShield) != null && base.Available();
        }
    }
}
