using System.Collections.Generic;
using Verse;

namespace ReinforcedMechanoids
{
    public class ReinforcedMechanoidsSettings : ModSettings
    {
        internal static float powerOutput = 5000f;

        internal static float marketValue = 2000f;

        public static bool dropWeaponOnDeath = false;

        public static List<string> disabledMechanoids = new List<string>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref powerOutput, "powerOutput", 5000f);
            Scribe_Values.Look(ref marketValue, "marketValue", 2000f);
            Scribe_Values.Look(ref dropWeaponOnDeath, "dropWeaponOnDeath", false);
            Scribe_Collections.Look(ref disabledMechanoids, "disabledMechanoids", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (disabledMechanoids is null)
                {
                    disabledMechanoids = new List<string>();
                }
            }
        }
    }
}
