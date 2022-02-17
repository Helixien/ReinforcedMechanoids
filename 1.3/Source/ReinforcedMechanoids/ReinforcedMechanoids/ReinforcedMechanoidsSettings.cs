using Verse;

namespace ReinforcedMechanoids
{
    public class ReinforcedMechanoidsSettings : ModSettings
    {
        internal static float powerOutput = 5000f;

        internal static float marketValue = 2000f;

        public static bool dropWeaponOnDeath = false;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref powerOutput, "powerOutput", 5000f);
            Scribe_Values.Look(ref marketValue, "marketValue", 2000f);
            Scribe_Values.Look(ref dropWeaponOnDeath, "dropWeaponOnDeath", false);
        }
    }
}
