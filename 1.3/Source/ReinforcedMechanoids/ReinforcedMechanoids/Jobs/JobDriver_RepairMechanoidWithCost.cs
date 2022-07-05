using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.XR;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ReinforcedMechanoids
{
	public class JobDriver_RepairMechanoidWithCost : JobDriver
	{
		public int totalWorkTick;
		private int workDone;
		protected Pawn MechanoidToRepair => (Pawn)job.GetTarget(TargetIndex.C).Thing;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			base.Map.reservationManager.ReleaseAllForTarget(MechanoidToRepair);
			if (!pawn.Reserve(job.GetTarget(TargetIndex.C), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
			foreach (var target in job.GetTargetQueue(TargetIndex.B))
			{
				pawn.Map.physicalInteractionReservationManager.Reserve(pawn, job, target.Thing);
			}
			return true;
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			yield return new Toil
			{
				initAction = delegate
				{
					workDone = 0;
					totalWorkTick = 8000 - (pawn.skills.GetSkill(SkillDefOf.Crafting).Level * 200);
					PawnUtility.ForceWait(MechanoidToRepair, totalWorkTick, this.pawn);
				}
			};
			this.FailOnDespawnedNullOrForbidden(TargetIndex.C);
			this.FailOnBurningImmobile(TargetIndex.C);
			Toil gotoMechanoid = Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.Touch)
				.FailOnDespawnedOrNull(TargetIndex.C)
				.FailOn(() => !pawn.CanReach(job.GetTarget(TargetIndex.C), PathEndMode.Touch, Danger.None))
				.FailOnSomeonePhysicallyInteracting(TargetIndex.C);

			if (job.targetQueueB != null)
			{
				yield return Toils_Jump.JumpIf(gotoMechanoid, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
				foreach (Toil item in CollectIngredientsToils(TargetIndex.B, TargetIndex.A, TargetIndex.A))
				{
					yield return item;
				}
			}
			yield return gotoMechanoid;
			yield return new Toil
			{
				initAction = delegate
				{
					PawnUtility.ForceWait(MechanoidToRepair, totalWorkTick, this.pawn);
					if (job.targetQueueB != null && job.placedThings != null)
					{
						for (var i = job.placedThings.Count - 1; i >= 0; i--)
						{
							job.placedThings[i].thing?.Destroy();
						}
						pawn.Map.physicalInteractionReservationManager.ReleaseClaimedBy(pawn, job);
						job.placedThings = null;
					}
				},
				tickAction = delegate
				{
					workDone++;
					if (workDone >= totalWorkTick)
					{
						var mech = MechanoidToRepair;
						switch (mech.GetComp<CompRepairable>().currentRepairingMode)
                        {
							case RepairingMode.Full:
                                {
									var hediffs = mech.health.hediffSet.hediffs;
									for (var i = hediffs.Count - 1; i >= 0; i--)
									{
										var hediff = hediffs[i];
										if (hediff is Hediff_MissingPart hediff_MissingPart)
										{
											var part = hediff_MissingPart.Part;
											mech.health.RemoveHediff(hediff);
											mech.health.RestorePart(part);
										}
										else if (hediff is Hediff_Injury)
										{
											mech.health.RemoveHediff(hediff);
										}
									}
									var improvisedHediff = mech.health.hediffSet.GetFirstHediffOfDef(RM_DefOf.RM_ImprovisedRepairs);
									if (improvisedHediff != null)
									{
										mech.health.RemoveHediff(improvisedHediff);
									}
									break;
								}
							case RepairingMode.Improvised:
                                {
									var hediffs = mech.health.hediffSet.hediffs;
									for (var i = hediffs.Count - 1; i >= 0; i--)
									{
										var hediff = hediffs[i];
										if (hediff is Hediff_MissingPart hediff_MissingPart)
										{
											var part = hediff_MissingPart.Part;
											mech.health.RemoveHediff(hediff);
											mech.health.RestorePart(part);
										}
										if (hediff is Hediff_Injury)
										{
											mech.health.RemoveHediff(hediff);
										}
									}
									var improvisedHediff = mech.health.hediffSet.GetFirstHediffOfDef(RM_DefOf.RM_ImprovisedRepairs);
									if (improvisedHediff == null)
									{
										improvisedHediff = HediffMaker.MakeHediff(RM_DefOf.RM_ImprovisedRepairs, mech);
										mech.health.AddHediff(improvisedHediff);
									}
									break;
								}
                        }
						this.EndJobWith(JobCondition.Succeeded);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Never
			}.WithEffect(() => EffecterDefOf.ConstructMetal, TargetIndex.C)
			 .PlaySustainerOrSound(() => RM_DefOf.Recipe_Machining);

			this.AddFinishAction(delegate
			{
				MechanoidToRepair.GetComp<CompRepairable>().currentRepairingMode = RepairingMode.None;
			});
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref totalWorkTick, "totalWorkTick");
			Scribe_Values.Look(ref workDone, "workDone");
		}
		public IEnumerable<Toil> CollectIngredientsToils(TargetIndex ingredientInd, TargetIndex billGiverInd, TargetIndex ingredientPlaceCellInd, bool subtractNumTakenFromJobCount = false, bool failIfStackCountLessThanJobCount = true)
		{
			Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(ingredientInd);
			yield return extract;
			Toil getToHaulTarget = Toils_Goto.GotoThing(ingredientInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(ingredientInd).FailOnSomeonePhysicallyInteracting(ingredientInd);
			yield return getToHaulTarget;
			yield return Toils_Haul.StartCarryThing(ingredientInd, putRemainderInQueue: true, subtractNumTakenFromJobCount, failIfStackCountLessThanJobCount);
			yield return JobDriver_DoBill.JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
			yield return Toils_Goto.GotoThing(billGiverInd, PathEndMode.Touch).FailOnDestroyedOrNull(ingredientInd);
			Toil findPlaceTarget = SetTargetToIngredientPlaceCell(ingredientInd, ingredientPlaceCellInd);
			yield return findPlaceTarget;
			yield return PlaceHauledThingInCell(ingredientPlaceCellInd, findPlaceTarget, storageMode: false);
			yield return Toils_Jump.JumpIfHaveTargetInQueue(ingredientInd, extract);
		}

		public static Toil SetTargetToIngredientPlaceCell(TargetIndex carryItemInd, TargetIndex cellTargetInd)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				Thing thing = curJob.GetTarget(carryItemInd).Thing;
				IntVec3 intVec = IntVec3.Invalid;
				foreach (IntVec3 item in IngredientPlaceCellsInOrder(curJob.GetTarget(TargetIndex.C).Thing))
				{
					if (!intVec.IsValid)
					{
						intVec = item;
					}
					bool flag = false;
					List<Thing> list = actor.Map.thingGrid.ThingsListAt(item);
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].def.category == ThingCategory.Item && (list[i].def != thing.def || list[i].stackCount == list[i].def.stackLimit))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						curJob.SetTarget(cellTargetInd, item);
						return;
					}
				}
				curJob.SetTarget(cellTargetInd, intVec);
			};
			return toil;
		}

		private static List<IntVec3> yieldedIngPlaceCells = new List<IntVec3>();
		private static IEnumerable<IntVec3> IngredientPlaceCellsInOrder(Thing destination)
		{
			yieldedIngPlaceCells.Clear();
			try
			{
				IntVec3 interactCell = destination.Position;
				IBillGiver billGiver = destination as IBillGiver;
				if (billGiver != null)
				{
					interactCell = ((Thing)billGiver).InteractionCell;
					foreach (IntVec3 item in billGiver.IngredientStackCells.OrderBy((IntVec3 c) => (c - interactCell).LengthHorizontalSquared))
					{
						yieldedIngPlaceCells.Add(item);
						yield return item;
					}
				}
				for (int i = 0; i < 200; i++)
				{
					IntVec3 intVec = interactCell + GenRadial.RadialPattern[i];
					if (!yieldedIngPlaceCells.Contains(intVec))
					{
						Building edifice = intVec.GetEdifice(destination.Map);
						if (edifice == null || edifice.def.passability != Traversability.Impassable || edifice.def.surfaceType != 0)
						{
							yield return intVec;
						}
					}
				}
			}
			finally
			{
				yieldedIngPlaceCells.Clear();
			}
		}

		public static Toil PlaceHauledThingInCell(TargetIndex cellInd, Toil nextToilOnPlaceFailOrIncomplete, bool storageMode, bool tryStoreInSameStorageIfSpotCantHoldWholeStack = false)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				IntVec3 cell = curJob.GetTarget(cellInd).Cell;
				if (actor.carryTracker.CarriedThing == null)
				{
					Log.Error(string.Concat(actor, " tried to place hauled thing in cell but is not hauling anything."));
				}
				else
				{
					SlotGroup slotGroup = actor.Map.haulDestinationManager.SlotGroupAt(cell);
					if (slotGroup != null && slotGroup.Settings.AllowedToAccept(actor.carryTracker.CarriedThing))
					{
						actor.Map.designationManager.TryRemoveDesignationOn(actor.carryTracker.CarriedThing, DesignationDefOf.Haul);
					}
					Action<Thing, int> placedAction = delegate (Thing th, int added)
					{
						if (curJob.placedThings == null)
						{
							curJob.placedThings = new List<ThingCountClass>();
						}
						ThingCountClass thingCountClass = curJob.placedThings.Find((ThingCountClass x) => x.thing == th);
						if (thingCountClass != null)
						{
							thingCountClass.Count += added;
						}
						else
						{
							curJob.placedThings.Add(new ThingCountClass(th, added));
						}
					};

					if (!actor.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out var _, placedAction))
					{
						if (storageMode)
						{
							if (nextToilOnPlaceFailOrIncomplete != null && ((tryStoreInSameStorageIfSpotCantHoldWholeStack && StoreUtility.TryFindBestBetterStoreCellForIn(actor.carryTracker.CarriedThing, actor, actor.Map, StoragePriority.Unstored, actor.Faction, cell.GetSlotGroup(actor.Map), out var foundCell)) || StoreUtility.TryFindBestBetterStoreCellFor(actor.carryTracker.CarriedThing, actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell)))
							{
								if (actor.CanReserve(foundCell))
								{
									actor.Reserve(foundCell, actor.CurJob);
								}
								actor.CurJob.SetTarget(cellInd, foundCell);
								actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
							}
							else
							{
								Job job = HaulAIUtility.HaulAsideJobFor(actor, actor.carryTracker.CarriedThing);
								if (job != null)
								{
									curJob.targetA = job.targetA;
									curJob.targetB = job.targetB;
									curJob.targetC = job.targetC;
									curJob.count = job.count;
									curJob.haulOpportunisticDuplicates = job.haulOpportunisticDuplicates;
									curJob.haulMode = job.haulMode;
									actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
								}
								else
								{
									Log.Error(string.Concat("Incomplete haul for ", actor, ": Could not find anywhere to put ", actor.carryTracker.CarriedThing, " near ", actor.Position, ". Destroying. This should never happen!"));
									actor.carryTracker.CarriedThing.Destroy();
								}
							}
						}
						else if (nextToilOnPlaceFailOrIncomplete != null)
						{
							actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
						}
					}
				}
			};
			return toil;
		}
	}
}