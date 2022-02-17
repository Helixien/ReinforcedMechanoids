using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Verse;

namespace ReinforcedMechanoids
{
	public class CompProperties_CausesGameCondition_ClimateAdjuster : CompProperties_CausesGameCondition
	{
		public CompProperties_CausesGameCondition_ClimateAdjuster()
		{
			compClass = typeof(CompCauseGameCondition_TemperatureOffset);
		}
	}

	public class CompCauseGameConditionPowerDependent : CompCauseGameCondition
    {
		public CompPowerTrader compPower;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
			compPower = this.parent.TryGetComp<CompPowerTrader>();
		}

		[HarmonyPatch(typeof(CompCauseGameCondition), nameof(CompCauseGameCondition.Active), MethodType.Getter)]
		public static class CompCauseGameCondition_ActivePatch
        {
			public static void Postfix(ref bool __result, CompCauseGameCondition __instance)
            {
				if (__result && __instance is CompCauseGameConditionPowerDependent powerDependent)
                {
					if (!powerDependent.compPower.PowerOn)
                    {
						__result = false;
                    }
                }
            }
        }
    }
	public class CompCauseGameCondition_TemperatureOffset : CompCauseGameConditionPowerDependent
	{
		public float temperatureOffset;
		public new CompProperties_CausesGameCondition_ClimateAdjuster Props => (CompProperties_CausesGameCondition_ClimateAdjuster)props;
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref temperatureOffset, "temperatureOffset", 0f);
		}

		private string GetFloatStringWithSign(float val)
		{
			if (val < 0f)
			{
				return val.ToString("0");
			}
			return "+" + val.ToString("0");
		}

		public void SetTemperatureOffset(float offset)
		{
			temperatureOffset += offset;
			temperatureOffset = new FloatRange(-50, 50).ClampToRange(temperatureOffset);
			ReSetupAllConditions();
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "-50";
			command_Action.icon = ContentFinder<Texture2D>.Get("UI/Buttons/ChangeClimateMinusMax");
			command_Action.action = (Action)Delegate.Combine(command_Action.action, (Action)delegate
			{
				SetTemperatureOffset(-50f);
			});
			command_Action.disabled = !this.parent.GetComp<CompPowerTrader>().PowerOn;
			command_Action.disabledReason = "NoPower".Translate();
			command_Action.hotKey = KeyBindingDefOf.Misc1;
			yield return command_Action;
			Command_Action command_Action2 = new Command_Action();
			command_Action2.defaultLabel = "-10";
			command_Action2.disabled = !this.parent.GetComp<CompPowerTrader>().PowerOn;
			command_Action2.disabledReason = "NoPower".Translate();
			command_Action2.icon = ContentFinder<Texture2D>.Get("UI/Buttons/ChangeClimateMinusMin");

			command_Action2.action = (Action)Delegate.Combine(command_Action2.action, (Action)delegate
			{
				SetTemperatureOffset(-10f);
			});
			command_Action2.hotKey = KeyBindingDefOf.Misc2;
			yield return command_Action2;
			Command_Action command_Action3 = new Command_Action();
			command_Action3.defaultLabel = "+10";
			command_Action3.icon = ContentFinder<Texture2D>.Get("UI/Buttons/ChangeClimatePlusMin");
			command_Action3.disabled = !this.parent.GetComp<CompPowerTrader>().PowerOn;
			command_Action3.disabledReason = "NoPower".Translate();
			command_Action3.action = (Action)Delegate.Combine(command_Action3.action, (Action)delegate
			{
				SetTemperatureOffset(10f);
			});
			command_Action3.hotKey = KeyBindingDefOf.Misc3;
			yield return command_Action3;
			Command_Action command_Action4 = new Command_Action();
			command_Action4.defaultLabel = "+50";
			command_Action4.disabled = !this.parent.GetComp<CompPowerTrader>().PowerOn;
			command_Action4.disabledReason = "NoPower".Translate();
			command_Action4.icon = ContentFinder<Texture2D>.Get("UI/Buttons/ChangeClimatePlusMax");
			command_Action4.action = (Action)Delegate.Combine(command_Action4.action, (Action)delegate
			{
				SetTemperatureOffset(50f);
			});
			command_Action4.hotKey = KeyBindingDefOf.Misc4;
			yield return command_Action4;
		}

		public override string CompInspectStringExtra()
		{
			string text = base.CompInspectStringExtra();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			return text + ("Temperature".Translate() + ": " + GetFloatStringWithSign(temperatureOffset));
		}

		public override void SetupCondition(GameCondition condition, Map map)
		{
			base.SetupCondition(condition, map);
			var tempCondition = ((GameCondition_TemperatureOffset)condition);
			tempCondition.tempOffset = temperatureOffset;
		}
	}
}
