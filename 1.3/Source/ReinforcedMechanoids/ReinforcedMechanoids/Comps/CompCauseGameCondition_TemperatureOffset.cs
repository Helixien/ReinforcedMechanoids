﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ReinforcedMechanoids
{
	public class CompProperties_CausesGameCondition_ClimateAdjuster : CompProperties_CausesGameCondition
	{
		public FloatRange temperatureOffsetRange = new FloatRange(-10f, 10f);

		public CompProperties_CausesGameCondition_ClimateAdjuster()
		{
			compClass = typeof(CompCauseGameCondition_TemperatureOffset);
		}
	}

	public class CompCauseGameCondition_TemperatureOffset : CompCauseGameCondition
	{
		public float temperatureOffset;

		private const float MaxTempForMinOffset = -5f;

		private const float MinTempForMaxOffset = 20f;

		public new CompProperties_CausesGameCondition_ClimateAdjuster Props => (CompProperties_CausesGameCondition_ClimateAdjuster)props;

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			temperatureOffset = Props.temperatureOffsetRange.min;
		}

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
			temperatureOffset = Props.temperatureOffsetRange.ClampToRange(offset);
			Log.Message("temperatureOffset: " + temperatureOffset);
			ReSetupAllConditions();
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "-50";
			command_Action.icon = ContentFinder<Texture2D>.Get("UI/Buttons/ChangeClimateMinusMax");
			command_Action.action = (Action)Delegate.Combine(command_Action.action, (Action)delegate
			{
				SetTemperatureOffset(temperatureOffset - 50f);
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
				SetTemperatureOffset(temperatureOffset - 10f);
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
				SetTemperatureOffset(temperatureOffset + 10f);
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
				SetTemperatureOffset(temperatureOffset + 50f);
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
			((GameCondition_TemperatureOffset)condition).tempOffset = temperatureOffset;
		}

		public override void RandomizeSettings(Site site)
		{
			bool flag = false;
			bool flag2 = false;
			foreach (WorldObject allWorldObject in Find.WorldObjects.AllWorldObjects)
			{
				Settlement settlement;
				if ((settlement = (allWorldObject as Settlement)) != null && settlement.Faction == Faction.OfPlayer)
				{
					if (settlement.Map != null)
					{
						bool flag3 = false;
						foreach (GameCondition activeCondition in settlement.Map.GameConditionManager.ActiveConditions)
						{
							if (activeCondition is GameCondition_TemperatureOffset)
							{
								float num = activeCondition.TemperatureOffset();
								if (num > 0f)
								{
									flag3 = true;
									flag = true;
									flag2 = false;
								}
								else if (num < 0f)
								{
									flag3 = true;
									flag2 = true;
									flag = false;
								}
								if (flag3)
								{
									break;
								}
							}
						}
						if (flag3)
						{
							break;
						}
					}
					int tile = allWorldObject.Tile;
					if ((float)Find.WorldGrid.TraversalDistanceBetween(site.Tile, tile, passImpassable: true, Props.worldRange + 1) <= (float)Props.worldRange)
					{
						float num2 = GenTemperature.MinTemperatureAtTile(tile);
						float num3 = GenTemperature.MaxTemperatureAtTile(tile);
						if (num2 < -5f)
						{
							flag2 = true;
						}
						if (num3 > 20f)
						{
							flag = true;
						}
					}
				}
			}
			if (flag2 == flag)
			{
				temperatureOffset = (Rand.Bool ? Props.temperatureOffsetRange.min : Props.temperatureOffsetRange.max);
			}
			else if (flag2)
			{
				temperatureOffset = Props.temperatureOffsetRange.min;
			}
			else if (flag)
			{
				temperatureOffset = Props.temperatureOffsetRange.max;
			}
		}
	}
}
