using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.TutorUtility;

namespace LWM.DeepStorage
{
    public class DSGUI_AHLO
    {
        private static void AddHumanlikeOrdersForThing(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            var c = IntVec3.FromVector3(clickPos);
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                foreach (var dest in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), true))
                {
                    var flag = dest.HasThing && dest.Thing is Pawn && ((Pawn) dest.Thing).IsWildMan();
                    if (pawn.Drafted || flag)
                    {
                        if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else
                        {
                            var pTarg = (Pawn) dest.Thing;
                            var action = (Action) (() =>
                            {
                                var buildingBed = RestUtility.FindBedFor(pTarg, pawn, true, false) ?? RestUtility.FindBedFor(pTarg, pawn, true, false, true);
                                if (buildingBed == null)
                                {
                                    Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg, MessageTypeDefOf.RejectInput, false);
                                }
                                else
                                {
                                    var job = JobMaker.MakeJob(JobDefOf.Arrest, (LocalTargetInfo) pTarg, (LocalTargetInfo) buildingBed);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                    if (pTarg.Faction == null || (pTarg.Faction == Faction.OfPlayer || pTarg.Faction.def.hidden) && !pTarg.IsQuestLodger())
                                        return;
                                    
                                    DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, Array.Empty<string>());
                                }
                            });
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                                new FloatMenuOption(
                                    "TryToArrest".Translate((NamedArgument) dest.Thing.LabelCap, (NamedArgument) dest.Thing,
                                        (NamedArgument) pTarg.GetAcceptArrestChance(pawn).ToStringPercent()), action, MenuOptionPriority.High, null, dest.Thing), pawn,
                                (LocalTargetInfo) pTarg));
                        }
                    }
                }

            foreach (var thing in c.GetThingList(pawn.Map))
            {
                var t = thing;
                if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                {
                    var label = !t.def.ingestible.ingestCommandString.NullOrEmpty()
                        ? string.Format(t.def.ingestible.ingestCommandString, t.LabelShort)
                        : (string) "ConsumeThing".Translate((NamedArgument) t.LabelShort, (NamedArgument) t);
                    if (!t.IsSociallyProper(pawn))
                        label = label + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
                    FloatMenuOption floatMenuOption;
                    if (t.def.IsNonMedicalDrug && pawn.IsTeetotaler())
                    {
                        floatMenuOption = new FloatMenuOption(label + ": " + TraitDefOf.DrugDesire.DataAtDegree(-1).LabelCap, null);
                    }
                    else if (FoodUtility.InappropriateForTitle(t.def, pawn, true))
                    {
                        floatMenuOption = new FloatMenuOption(
                            label + ": " + "FoodBelowTitleRequirements".Translate((NamedArgument) pawn.royalty.MostSeniorTitle.def.GetLabelFor(pawn)), null);
                    }
                    else if (!pawn.CanReach((LocalTargetInfo) t, PathEndMode.OnCell, Danger.Deadly))
                    {
                        floatMenuOption = new FloatMenuOption(label + ": " + "NoPath".Translate(), null);
                    }
                    else
                    {
                        var priority = t is Corpse ? MenuOptionPriority.Low : MenuOptionPriority.Default;
                        var maxAmountToPickup1 = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
                        floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
                        {
                            var maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
                            if (maxAmountToPickup == 0)
                                return;
                            t.SetForbidden(false);
                            var job = JobMaker.MakeJob(JobDefOf.Ingest, (LocalTargetInfo) t);
                            job.count = maxAmountToPickup;
                            pawn.jobs.TryTakeOrderedJob(job);
                        }, priority), pawn, (LocalTargetInfo) t);
                        if (maxAmountToPickup1 == 0)
                            floatMenuOption.action = null;
                    }

                    opts.Add(floatMenuOption);
                }
            }

            foreach (var dest in GenUI.TargetsAt(clickPos, TargetingParameters.ForQuestPawnsWhoWillJoinColony(pawn), true))
            {
                var toHelpPawn = (Pawn) dest.Thing;
                var floatMenuOption = pawn.CanReach(dest, PathEndMode.Touch, Danger.Deadly)
                    ? FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption(toHelpPawn.IsPrisoner ? "FreePrisoner".Translate() : "OfferHelp".Translate(),
                            () => pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.OfferHelp, (LocalTargetInfo) toHelpPawn)), MenuOptionPriority.RescueOrCapture, null,
                            toHelpPawn), pawn, (LocalTargetInfo) toHelpPawn)
                    : new FloatMenuOption("CannotGoNoPath".Translate(), null);
                opts.Add(floatMenuOption);
            }

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (var thing in c.GetThingList(pawn.Map))
                {
                    var corpse = thing as Corpse;
                    if (corpse != null && corpse.IsInValidStorage())
                    {
                        var priority = StoreUtility.CurrentHaulDestinationOf(corpse).GetStoreSettings().Priority;
                        IHaulDestination haulDestination;
                        if (StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(corpse, pawn, pawn.Map, priority, Faction.OfPlayer, out haulDestination, true) &&
                            haulDestination.GetStoreSettings().Priority == priority && haulDestination is Building_Grave)
                        {
                            var grave = haulDestination as Building_Grave;
                            var label = (string) "PrioritizeGeneric".Translate("Burying".Translate(), (NamedArgument) corpse.Label);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                                new FloatMenuOption(label, () => pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse, grave))), pawn,
                                new LocalTargetInfo(corpse)));
                        }
                    }
                }

                foreach (var localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    var victim = (Pawn) localTargetInfo.Thing;
                    if (!victim.InBed() && pawn.CanReserveAndReach((LocalTargetInfo) victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) &&
                        !victim.mindState.WillJoinColonyIfRescued)
                    {
                        if (!victim.IsPrisonerOfColony && !victim.InMentalState &&
                            (victim.Faction == Faction.OfPlayer || victim.Faction == null || !victim.Faction.HostileTo(Faction.OfPlayer)))
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate((NamedArgument) victim.LabelCap, (NamedArgument) victim), () =>
                            {
                                var buildingBed = RestUtility.FindBedFor(victim, pawn, false, false) ?? RestUtility.FindBedFor(victim, pawn, false, false, true);
                                if (buildingBed == null)
                                {
                                    Messages.Message(
                                        "CannotRescue".Translate() + ": " +
                                        (!victim.RaceProps.Animal ? (string) "NoNonPrisonerBed".Translate() : (string) "NoAnimalBed".Translate()), victim,
                                        MessageTypeDefOf.RejectInput, false);
                                }
                                else
                                {
                                    var job = JobMaker.MakeJob(JobDefOf.Rescue, (LocalTargetInfo) victim, (LocalTargetInfo) buildingBed);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, (LocalTargetInfo) victim));
                        if (victim.RaceProps.Humanlike && (victim.InMentalState || victim.Faction != Faction.OfPlayer ||
                                                           victim.Downed && (victim.guilt.IsGuilty || victim.IsPrisonerOfColony)))
                        {
                            var taggedString = "Capture".Translate((NamedArgument) victim.LabelCap, (NamedArgument) victim);
                            if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.def.hidden && !victim.Faction.HostileTo(Faction.OfPlayer) &&
                                !victim.IsPrisonerOfColony)
                                taggedString += ": " + "AngersFaction".Translate().CapitalizeFirst();
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString, () =>
                            {
                                var buildingBed = RestUtility.FindBedFor(victim, pawn, true, false) ?? RestUtility.FindBedFor(victim, pawn, true, false, true);
                                if (buildingBed == null)
                                {
                                    Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageTypeDefOf.RejectInput, false);
                                }
                                else
                                {
                                    var job = JobMaker.MakeJob(JobDefOf.Capture, (LocalTargetInfo) victim, (LocalTargetInfo) buildingBed);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                                    if (victim.Faction == null || victim.Faction == Faction.OfPlayer || victim.Faction.def.hidden || victim.Faction.HostileTo(Faction.OfPlayer) ||
                                        victim.IsPrisonerOfColony)
                                        return;
                                    Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN")).AdjustedFor(victim), victim, MessageTypeDefOf.CautionInput,
                                        false);
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, (LocalTargetInfo) victim));
                        }
                    }
                }

                foreach (var localTargetInfo1 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    var localTargetInfo2 = localTargetInfo1;
                    var victim = (Pawn) localTargetInfo2.Thing;
                    if (victim.Downed && pawn.CanReserveAndReach((LocalTargetInfo) victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) &&
                        Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn, true) != null)
                    {
                        var label1 = (string) "CarryToCryptosleepCasket".Translate((NamedArgument) localTargetInfo2.Thing.LabelCap, (NamedArgument) localTargetInfo2.Thing);
                        var jDef = JobDefOf.CarryToCryptosleepCasket;
                        var action = (Action) (() =>
                        {
                            var cryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn) ??
                                                    Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn, true);
                            if (cryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim, MessageTypeDefOf.RejectInput,
                                    false);
                            }
                            else
                            {
                                var job = JobMaker.MakeJob(jDef, (LocalTargetInfo) victim, (LocalTargetInfo) cryptosleepCasket);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }
                        });
                        if (!victim.IsQuestLodger())
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label1, action, MenuOptionPriority.Default, null, victim), pawn,
                                (LocalTargetInfo) victim));
                        }
                        else
                        {
                            var label2 = (string) (label1 + (" (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")"));
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label2, null, MenuOptionPriority.Default, null, victim), pawn,
                                (LocalTargetInfo) victim));
                        }
                    }
                }

                if (ModsConfig.RoyaltyActive)
                    foreach (var localTargetInfo1 in GenUI.TargetsAt(clickPos, TargetingParameters.ForShuttle(pawn), true))
                    {
                        var localTargetInfo2 = localTargetInfo1;
                        var victim = (Pawn) localTargetInfo2.Thing;
                        var validator = (Predicate<Thing>) (thing =>
                        {
                            var comp = thing.TryGetComp<CompShuttle>();
                            return comp != null && comp.IsAllowed(victim);
                        });
                        var shuttleThing = GenClosest.ClosestThingReachable(victim.Position, victim.Map, ThingRequest.ForDef(ThingDefOf.Shuttle), PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn), 9999f, validator);
                        if (shuttleThing != null && pawn.CanReserveAndReach((LocalTargetInfo) victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) &&
                            !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                        {
                            var label = (string) "CarryToShuttle".Translate((NamedArgument) localTargetInfo2.Thing);
                            var action = (Action) (() =>
                            {
                                var comp = shuttleThing.TryGetComp<CompShuttle>();
                                if (!comp.LoadingInProgressOrReadyToLaunch)
                                    TransporterUtility.InitiateLoading(Gen.YieldSingle(comp.Transporter));
                                var job = JobMaker.MakeJob(JobDefOf.HaulToTransporter, (LocalTargetInfo) victim, (LocalTargetInfo) shuttleThing);
                                job.ignoreForbidden = true;
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            });
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn, (LocalTargetInfo) victim));
                        }
                    }
            }

            foreach (var localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), true))
            {
                var stripTarg = localTargetInfo;
                var floatMenuOption = pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly)
                    ? stripTarg.Pawn == null || !stripTarg.Pawn.HasExtraHomeFaction() ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                        "Strip".Translate((NamedArgument) stripTarg.Thing.LabelCap, (NamedArgument) stripTarg.Thing), () =>
                        {
                            stripTarg.Thing.SetForbidden(false, false);
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, stripTarg));
                        }), pawn, stripTarg) :
                    new FloatMenuOption(
                        "CannotStrip".Translate((NamedArgument) stripTarg.Thing.LabelCap, (NamedArgument) stripTarg.Thing) + ": " + "QuestRelated".Translate().CapitalizeFirst(),
                        null)
                    : new FloatMenuOption("CannotStrip".Translate((NamedArgument) stripTarg.Thing.LabelCap, (NamedArgument) stripTarg.Thing) + ": " + "NoPath".Translate(), null);
                opts.Add(floatMenuOption);
            }

            if (pawn.equipment != null)
            {
                var equipment = (ThingWithComps) null;
                var thingList = c.GetThingList(pawn.Map);
                for (var index = 0; index < thingList.Count; ++index)
                    if (thingList[index].TryGetComp<CompEquippable>() != null)
                    {
                        equipment = (ThingWithComps) thingList[index];
                        break;
                    }

                if (equipment != null)
                {
                    var labelShort = equipment.LabelShort;
                    FloatMenuOption floatMenuOption;
                    if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        floatMenuOption = new FloatMenuOption(
                            "CannotEquip".Translate((NamedArgument) labelShort) + ": " +
                            "IsIncapableOfViolenceLower".Translate((NamedArgument) pawn.LabelShort, (NamedArgument) pawn), null);
                    }
                    else if (!pawn.CanReach((LocalTargetInfo) equipment, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        floatMenuOption = new FloatMenuOption("CannotEquip".Translate((NamedArgument) labelShort) + ": " + "NoPath".Translate(), null);
                    }
                    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        floatMenuOption = new FloatMenuOption("CannotEquip".Translate((NamedArgument) labelShort) + ": " + "Incapable".Translate(), null);
                    }
                    else if (equipment.IsBurning())
                    {
                        floatMenuOption = new FloatMenuOption("CannotEquip".Translate((NamedArgument) labelShort) + ": " + "BurningLower".Translate(), null);
                    }
                    else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(equipment, pawn))
                    {
                        floatMenuOption = new FloatMenuOption("CannotEquip".Translate((NamedArgument) labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
                    }
                    else
                    {
                        string cantReason;
                        if (!EquipmentUtility.CanEquip(equipment, pawn, out cantReason))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate((NamedArgument) labelShort) + ": " + cantReason.CapitalizeFirst(), null);
                        }
                        else
                        {
                            var label = (string) "Equip".Translate((NamedArgument) labelShort);
                            if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                                label = label + (" " + "EquipWarningBrawler".Translate());
                            floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
                            {
                                var confirmationDialogText = ThingRequiringRoyalPermissionUtility.GetEquipWeaponConfirmationDialogText(equipment, pawn);
                                var comp = equipment.TryGetComp<CompBladelinkWeapon>();
                                if (comp != null && comp.bondedPawn != pawn)
                                {
                                    if (!confirmationDialogText.NullOrEmpty())
                                        confirmationDialogText += "\n\n";
                                    confirmationDialogText += "BladelinkEquipWarning".Translate();
                                }

                                if (!confirmationDialogText.NullOrEmpty())
                                {
                                    confirmationDialogText += "\n\n" + "RoyalWeaponEquipConfirmation".Translate();
                                    Find.WindowStack.Add(new Dialog_MessageBox(confirmationDialogText, "Yes".Translate(), () => Equip(), "No".Translate()));
                                }
                                else
                                {
                                    Equip();
                                }
                            }, MenuOptionPriority.High), pawn, (LocalTargetInfo) equipment);
                        }
                    }

                    opts.Add(floatMenuOption);
                }

                void Equip()
                {
                    equipment.SetForbidden(false);
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, (LocalTargetInfo) equipment));
                    MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                }
            }

            TaggedString taggedString1;
            if (pawn.apparel != null)
            {
                var apparel = pawn.Map.thingGrid.ThingAt<Apparel>(c);
                if (apparel != null)
                {
                    FloatMenuOption floatMenuOption;
                    if (!pawn.CanReach((LocalTargetInfo) apparel, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        floatMenuOption = new FloatMenuOption("CannotWear".Translate((NamedArgument) apparel.Label, (NamedArgument) apparel) + ": " + "NoPath".Translate(), null);
                    }
                    else if (apparel.IsBurning())
                    {
                        floatMenuOption = new FloatMenuOption("CannotWear".Translate((NamedArgument) apparel.Label, (NamedArgument) apparel) + ": " + "Burning".Translate(), null);
                    }
                    else if (pawn.apparel.WouldReplaceLockedApparel(apparel))
                    {
                        var taggedString2 = "CannotWear".Translate((NamedArgument) apparel.Label, (NamedArgument) apparel) + ": ";
                        taggedString1 = "WouldReplaceLockedApparel".Translate();
                        var taggedString3 = taggedString1.CapitalizeFirst();
                        floatMenuOption = new FloatMenuOption(taggedString2 + taggedString3, null);
                    }
                    else
                    {
                        string cantReason;
                        floatMenuOption = ApparelUtility.HasPartsToWear(pawn, apparel.def)
                            ? EquipmentUtility.CanEquip(apparel, pawn, out cantReason) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                "ForceWear".Translate((NamedArgument) apparel.LabelShort, (NamedArgument) apparel), () =>
                                {
                                    apparel.SetForbidden(false);
                                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Wear, (LocalTargetInfo) apparel));
                                }, MenuOptionPriority.High), pawn, (LocalTargetInfo) apparel) :
                            new FloatMenuOption("CannotWear".Translate((NamedArgument) apparel.Label, (NamedArgument) apparel) + ": " + cantReason, null)
                            : new FloatMenuOption(
                                "CannotWear".Translate((NamedArgument) apparel.Label, (NamedArgument) apparel) + ": " + "CannotWearBecauseOfMissingBodyParts".Translate(), null);
                    }

                    opts.Add(floatMenuOption);
                }
            }

            if (pawn.IsFormingCaravan())
            {
                var item = c.GetFirstItem(pawn.Map);
                if (item != null && item.def.EverHaulable)
                {
                    var packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
                    var jobDef = packTarget == pawn ? JobDefOf.TakeInventory : JobDefOf.GiveToPackAnimal;
                    if (!pawn.CanReach((LocalTargetInfo) item, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "NoPath".Translate(), null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "TooHeavy".Translate(), null));
                    }
                    else
                    {
                        var capacityLeft = CaravanFormingUtility.CapacityLeft((LordJob_FormAndSendCaravan) pawn.GetLord().LordJob);
                        if (item.stackCount == 1)
                        {
                            var capacityLeft1 = capacityLeft - item.GetStatValue(StatDefOf.Mass);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravan".Translate((NamedArgument) item.Label, (NamedArgument) item), capacityLeft1), () =>
                                {
                                    item.SetForbidden(false, false);
                                    var job = JobMaker.MakeJob(jobDef, (LocalTargetInfo) item);
                                    job.count = 1;
                                    job.checkEncumbrance = packTarget == pawn;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item, item.stackCount))
                            {
                                opts.Add(new FloatMenuOption("CannotLoadIntoCaravanAll".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "TooHeavy".Translate(),
                                    null));
                            }
                            else
                            {
                                var capacityLeft1 = capacityLeft - item.stackCount * item.GetStatValue(StatDefOf.Mass);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                    CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanAll".Translate((NamedArgument) item.Label, (NamedArgument) item), capacityLeft1),
                                    () =>
                                    {
                                        item.SetForbidden(false, false);
                                        var job = JobMaker.MakeJob(jobDef, (LocalTargetInfo) item);
                                        job.count = item.stackCount;
                                        job.checkEncumbrance = packTarget == pawn;
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    }, MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                            }

                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                    "LoadIntoCaravanSome".Translate((NamedArgument) item.LabelNoCount, (NamedArgument) item), () => Find.WindowStack.Add(new Dialog_Slider(val =>
                                    {
                                        float capacityLeft = capacityLeft - val * item.GetStatValue(StatDefOf.Mass);
                                        return CaravanFormingUtility.AppendOverweightInfo(
                                            string.Format("LoadIntoCaravanCount".Translate((NamedArgument) item.LabelNoCount, (NamedArgument) item), val), capacityLeft);
                                    }, 1, Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, item), item.stackCount), closure_1 ?? (closure_1 =
                                        (Action<int>) (count =>
                                        {
                                            item.SetForbidden(false, false);
                                            var job = JobMaker.MakeJob(jobDef,
                                                (LocalTargetInfo) item);
                                            job.count = count;
                                            job.checkEncumbrance =
                                                packTarget == pawn;
                                            pawn.jobs.TryTakeOrderedJob(job);
                                        })))), MenuOptionPriority.High), pawn,
                                (LocalTargetInfo) item));
                        }
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
            {
                var item = c.GetFirstItem(pawn.Map);
                if (item != null && item.def.EverHaulable)
                {
                    if (!pawn.CanReach((LocalTargetInfo) item, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "NoPath".Translate(), null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "TooHeavy".Translate(), null));
                    }
                    else if (item.stackCount == 1)
                    {
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUp".Translate((NamedArgument) item.Label, (NamedArgument) item), () =>
                        {
                            item.SetForbidden(false, false);
                            var job = JobMaker.MakeJob(JobDefOf.TakeInventory, (LocalTargetInfo) item);
                            job.count = 1;
                            job.checkEncumbrance = true;
                            pawn.jobs.TryTakeOrderedJob(job);
                        }, MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                    }
                    else
                    {
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, item.stackCount))
                            opts.Add(new FloatMenuOption("CannotPickUpAll".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "TooHeavy".Translate(), null));
                        else
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpAll".Translate((NamedArgument) item.Label, (NamedArgument) item), () =>
                            {
                                item.SetForbidden(false, false);
                                var job = JobMaker.MakeJob(JobDefOf.TakeInventory, (LocalTargetInfo) item);
                                job.count = item.stackCount;
                                job.checkEncumbrance = true;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }, MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpSome".Translate((NamedArgument) item.LabelNoCount, (NamedArgument) item), () =>
                            Find.WindowStack.Add(new Dialog_Slider("PickUpCount".Translate((NamedArgument) item.LabelNoCount, (NamedArgument) item), 1,
                                Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item), item.stackCount), count =>
                                {
                                    item.SetForbidden(false, false);
                                    var job = JobMaker.MakeJob(JobDefOf.TakeInventory, (LocalTargetInfo) item);
                                    job.count = count;
                                    job.checkEncumbrance = true;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                })), MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
            {
                var item = c.GetFirstItem(pawn.Map);
                if (item != null && item.def.EverHaulable)
                {
                    var bestPackAnimal = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn);
                    if (bestPackAnimal != null)
                    {
                        if (!pawn.CanReach((LocalTargetInfo) item, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "NoPath".Translate(), null));
                        }
                        else if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, 1))
                        {
                            opts.Add(
                                new FloatMenuOption("CannotGiveToPackAnimal".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "TooHeavy".Translate(), null));
                        }
                        else if (item.stackCount == 1)
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimal".Translate((NamedArgument) item.Label, (NamedArgument) item),
                                () =>
                                {
                                    item.SetForbidden(false, false);
                                    var job = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, (LocalTargetInfo) item);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, item.stackCount))
                                opts.Add(new FloatMenuOption(
                                    "CannotGiveToPackAnimalAll".Translate((NamedArgument) item.Label, (NamedArgument) item) + ": " + "TooHeavy".Translate(), null));
                            else
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                    "GiveToPackAnimalAll".Translate((NamedArgument) item.Label, (NamedArgument) item), () =>
                                    {
                                        item.SetForbidden(false, false);
                                        var job = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, (LocalTargetInfo) item);
                                        job.count = item.stackCount;
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    }, MenuOptionPriority.High), pawn, (LocalTargetInfo) item));
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                    "GiveToPackAnimalSome".Translate((NamedArgument) item.LabelNoCount, (NamedArgument) item), () =>
                                        Find.WindowStack.Add(new Dialog_Slider("GiveToPackAnimalCount".Translate((NamedArgument) item.LabelNoCount, (NamedArgument) item), 1,
                                            Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(bestPackAnimal, item), item.stackCount), closure_4 ?? (closure_4 =
                                                (Action<int>) (count =>
                                                {
                                                    item.SetForbidden(false, false);
                                                    var job = JobMaker.MakeJob(
                                                        JobDefOf.GiveToPackAnimal,
                                                        (LocalTargetInfo) item);
                                                    job.count = count;
                                                    pawn.jobs.TryTakeOrderedJob(job);
                                                })))), MenuOptionPriority.High), pawn,
                                (LocalTargetInfo) item));
                        }
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && pawn.Map.exitMapGrid.MapUsesExitGrid)
                foreach (var target in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    var p = (Pawn) target.Thing;
                    if (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony || CaravanUtility.ShouldAutoCapture(p, Faction.OfPlayer))
                    {
                        if (!pawn.CanReach((LocalTargetInfo) p, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotCarryToExit".Translate((NamedArgument) p.Label, (NamedArgument) p) + ": " + "NoPath".Translate(), null));
                        }
                        else
                        {
                            IntVec3 exitSpot;
                            if (!RCellFinder.TryFindBestExitSpot(pawn, out exitSpot))
                            {
                                opts.Add(new FloatMenuOption("CannotCarryToExit".Translate((NamedArgument) p.Label, (NamedArgument) p) + ": " + "NoPath".Translate(), null));
                            }
                            else
                            {
                                var taggedString2 = p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony
                                    ? "CarryToExit".Translate((NamedArgument) p.Label, (NamedArgument) p)
                                    : "CarryToExitAndCapture".Translate((NamedArgument) p.Label, (NamedArgument) p);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString2, () =>
                                {
                                    var job = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToExit, (LocalTargetInfo) p, exitSpot);
                                    job.count = 1;
                                    job.failIfCantJoinOrCreateCaravan = true;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, MenuOptionPriority.High), pawn, target));
                            }
                        }
                    }
                }

            if (pawn.equipment != null && pawn.equipment.Primary != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true).Any())
            {
                if (pawn.IsQuestLodger())
                {
                    var floatMenuOptionList = opts;
                    var taggedString2 = "CannotDrop".Translate((NamedArgument) pawn.equipment.Primary.Label, (NamedArgument) pawn.equipment.Primary) + ": ";
                    taggedString1 = "QuestRelated".Translate();
                    var taggedString3 = taggedString1.CapitalizeFirst();
                    var floatMenuOption = new FloatMenuOption(taggedString2 + taggedString3, null);
                    floatMenuOptionList.Add(floatMenuOption);
                }
                else
                {
                    var action = (Action) (() => pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, (LocalTargetInfo) pawn.equipment.Primary)));
                    opts.Add(new FloatMenuOption("Drop".Translate((NamedArgument) pawn.equipment.Primary.Label, (NamedArgument) pawn.equipment.Primary), action,
                        MenuOptionPriority.Default, null, pawn));
                }
            }

            foreach (var dest in GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), true))
                if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + ": " + "NoPath".Translate(), null));
                }
                else if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
                {
                    opts.Add(new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null));
                }
                else if (!pawn.CanTradeWith(dest.Thing.Faction, ((Pawn) dest.Thing).TraderKind))
                {
                    opts.Add(new FloatMenuOption("CannotTradeMissingTitleAbility".Translate(), null));
                }
                else
                {
                    var pTarg = (Pawn) dest.Thing;
                    var action = (Action) (() =>
                    {
                        var job = JobMaker.MakeJob(JobDefOf.TradeWithPawn, (LocalTargetInfo) pTarg);
                        job.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                    });
                    var str = "";
                    if (pTarg.Faction != null)
                        str = " (" + pTarg.Faction.Name + ")";
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption("TradeWith".Translate((NamedArgument) (pTarg.LabelShort + ", " + pTarg.TraderKind.label)) + str, action,
                            MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, (LocalTargetInfo) pTarg));
                }

            foreach (var localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForOpen(pawn), true))
            {
                var casket = localTargetInfo;
                if (!pawn.CanReach(casket, PathEndMode.OnCell, Danger.Deadly))
                    opts.Add(new FloatMenuOption("CannotOpen".Translate((NamedArgument) casket.Thing) + ": " + "NoPath".Translate(), null));
                else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    opts.Add(new FloatMenuOption("CannotOpen".Translate((NamedArgument) casket.Thing) + ": " + "Incapable".Translate(), null));
                else if (casket.Thing.Map.designationManager.DesignationOn(casket.Thing, DesignationDefOf.Open) == null)
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Open".Translate((NamedArgument) casket.Thing), () =>
                    {
                        var job = JobMaker.MakeJob(JobDefOf.Open, (LocalTargetInfo) casket.Thing);
                        job.ignoreDesignations = true;
                        pawn.jobs.TryTakeOrderedJob(job);
                    }, MenuOptionPriority.High), pawn, (LocalTargetInfo) casket.Thing));
            }

            foreach (var thing in pawn.Map.thingGrid.ThingsAt(c))
            foreach (var floatMenuOption in thing.GetFloatMenuOptions(pawn))
                opts.Add(floatMenuOption);
        }
    }
}