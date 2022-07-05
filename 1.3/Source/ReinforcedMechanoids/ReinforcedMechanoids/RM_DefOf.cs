using RimWorld;
using System.Reflection;
using Verse;

namespace ReinforcedMechanoids
{
    [DefOf]
	public static class RM_DefOf
    {
		public static PawnKindDef RM_Mech_Caretaker;
		public static PawnKindDef RM_Mech_Vulture;
		public static PawnKindDef RM_Mech_Behemoth;
		public static JobDef RM_RepairMechanoid;
		public static JobDef RM_RepairThing;
		public static JobDef RM_FollowClose;
		public static BodyPartDef RM_BehemothShield;
		public static ThingDef RM_VanometricGenerator;
		public static ThingDef RM_VanometricMechanoidCell;
		public static HediffDef RM_BehemothAttack;
        public static JobDef RM_HackMechanoidCorpseAtMechanoidStation;
        public static JobDef RM_RepairPlayerMechanoid;
        public static ThinkTreeDef VFE_Mechanoids_Machine_RiddableConstant;
        public static ThinkTreeDef Downed;
        public static ThinkTreeDef RM_MechanoidHacked_Behaviour;
        public static ThinkTreeDef JoinAutoJoinableCaravan;
        public static ThinkTreeDef LordDutyConstant;
        public static DesignationDef RM_HackMechanoid;
        public static EffecterDef RM_Hacking;
        public static SoundDef Recipe_Machining;
        public static HediffDef RM_ImprovisedRepairs;
        public static JobDef RM_AttachTurretToMechanoid;
    }
}
