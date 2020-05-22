using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
        [UsedImplicitly]
        public static void AddHumanlikeOrdersForThing(Thing target, Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            // var c = IntVec3.FromVector3(clickPos);
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                foreach (var item8 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForArrest(pawn), true))
                {
                    var flag = item8.HasThing && item8.Thing is Pawn thing && thing.IsWildMan();
                    if (!pawn.Drafted && !flag) continue;

                    if (!pawn.CanReach(item8, PathEndMode.OnCell, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    }
                    else
                    {
                        var pTarg2 = (Pawn) item8.Thing;

                        void Action()
                        {
                            var building_Bed3 = RestUtility.FindBedFor(pTarg2, pawn, true, false) ?? RestUtility.FindBedFor(pTarg2, pawn, true, false, true);
                            if (building_Bed3 == null)
                            {
                                Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg2, MessageTypeDefOf.RejectInput, false);
                            }
                            else
                            {
                                var job19 = JobMaker.MakeJob(JobDefOf.Arrest, pTarg2, building_Bed3);
                                job19.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job19);
                                if (pTarg2.Faction != null && (pTarg2.Faction != Faction.OfPlayer && !pTarg2.Faction.def.hidden || pTarg2.IsQuestLodger()))
                                    DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies);
                            }
                        }

                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption("TryToArrest".Translate(item8.Thing.LabelCap, item8.Thing, pTarg2.GetAcceptArrestChance(pawn).ToStringPercent()), Action,
                                MenuOptionPriority.High, null, item8.Thing), pawn, pTarg2));
                    }
                }

            if (target.def.ingestible != null && pawn.RaceProps.CanEverEat(target) && target.IngestibleNow)
            {
                var text = !target.def.ingestible.ingestCommandString.NullOrEmpty()
                    ? string.Format(target.def.ingestible.ingestCommandString, target.LabelShort)
                    : (string) "ConsumeThing".Translate(target.LabelShort, target);
                if (!target.IsSociallyProper(pawn)) text = text + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
                FloatMenuOption floatMenuOption;
                if (target.def.IsNonMedicalDrug && pawn.IsTeetotaler())
                {
                    floatMenuOption = new FloatMenuOption(text + ": " + TraitDefOf.DrugDesire.DataAtDegree(-1).LabelCap, null);
                }
                else if (FoodUtility.InappropriateForTitle(target.def, pawn, true))
                {
                    floatMenuOption = new FloatMenuOption(text + ": " + "FoodBelowTitleRequirements".Translate(pawn.royalty.MostSeniorTitle.def.GetLabelFor(pawn)), null);
                }
                else if (!pawn.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                {
                    floatMenuOption = new FloatMenuOption(text + ": " + "NoPath".Translate(), null);
                }
                else
                {
                    var priority = target is Corpse ? MenuOptionPriority.Low : MenuOptionPriority.Default;
                    var maxAmountToPickup =
                        FoodUtility.GetMaxAmountToPickup(target, pawn, FoodUtility.WillIngestStackCountOf(pawn, target.def, target.GetStatValue(StatDefOf.Nutrition)));
                    floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
                    {
                        var maxAmountToPickup2 =
                            FoodUtility.GetMaxAmountToPickup(target, pawn, FoodUtility.WillIngestStackCountOf(pawn, target.def, target.GetStatValue(StatDefOf.Nutrition)));
                        if (maxAmountToPickup2 == 0) return;

                        target.SetForbidden(false);
                        var job18 = JobMaker.MakeJob(JobDefOf.Ingest, target);
                        job18.count = maxAmountToPickup2;
                        pawn.jobs.TryTakeOrderedJob(job18);
                    }, priority), pawn, target);
                    if (maxAmountToPickup == 0) floatMenuOption.action = null;
                }

                opts.Add(floatMenuOption);
            }

            opts.AddRange(from item9 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForQuestPawnsWhoWillJoinColony(pawn), true)
                let toHelpPawn = (Pawn) item9.Thing
                select pawn.CanReach(item9, PathEndMode.Touch, Danger.Deadly)
                    ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(toHelpPawn.IsPrisoner ? "FreePrisoner".Translate() : "OfferHelp".Translate(), delegate { pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.OfferHelp, toHelpPawn)); }, MenuOptionPriority.RescueOrCapture, null, toHelpPawn), pawn, toHelpPawn)
                    : new FloatMenuOption("CannotGoNoPath".Translate(), null));

            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                if (target is Corpse corpse && corpse.IsInValidStorage())
                {
                    var priority2 = StoreUtility.CurrentHaulDestinationOf(corpse).GetStoreSettings().Priority;
                    if (!StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(corpse, pawn, pawn.Map, priority2, Faction.OfPlayer, out var haulDestination, true) ||
                        haulDestination.GetStoreSettings().Priority != priority2 || !(haulDestination is Building_Grave)) return;

                    var grave = haulDestination as Building_Grave;
                    string label = "PrioritizeGeneric".Translate("Burying".Translate(), corpse.Label);
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption(label, delegate { pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse, grave)); }), pawn,
                        new LocalTargetInfo(corpse)));
                }

                foreach (var item10 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    var victim3 = (Pawn) item10.Thing;
                    if (victim3.InBed() || !pawn.CanReserveAndReach(victim3, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) ||
                        victim3.mindState.WillJoinColonyIfRescued) continue;
                    
                    if (!victim3.IsPrisonerOfColony && !victim3.InMentalState &&
                        (victim3.Faction == Faction.OfPlayer || victim3.Faction == null || !victim3.Faction.HostileTo(Faction.OfPlayer)))
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate(victim3.LabelCap, victim3), delegate
                        {
                            var building_Bed2 = RestUtility.FindBedFor(victim3, pawn, false, false) ?? RestUtility.FindBedFor(victim3, pawn, false, false, true);
                            if (building_Bed2 == null)
                            {
                                var t3 = !victim3.RaceProps.Animal ? (string) "NoNonPrisonerBed".Translate() : (string) "NoAnimalBed".Translate();
                                Messages.Message("CannotRescue".Translate() + ": " + t3, victim3, MessageTypeDefOf.RejectInput, false);
                            }
                            else
                            {
                                var job17 = JobMaker.MakeJob(JobDefOf.Rescue, victim3, building_Bed2);
                                job17.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job17);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                            }
                        }, MenuOptionPriority.RescueOrCapture, null, victim3), pawn, victim3));
                    if (!victim3.RaceProps.Humanlike || (!victim3.InMentalState && victim3.Faction == Faction.OfPlayer &&
                                                         (!victim3.Downed || (!victim3.guilt.IsGuilty && !victim3.IsPrisonerOfColony)))) continue;
                    
                    var taggedString = "Capture".Translate(victim3.LabelCap, victim3);
                    if (victim3.Faction != null && victim3.Faction != Faction.OfPlayer && !victim3.Faction.def.hidden && !victim3.Faction.HostileTo(Faction.OfPlayer) &&
                        !victim3.IsPrisonerOfColony) taggedString += ": " + "AngersFaction".Translate().CapitalizeFirst();
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString, delegate
                    {
                        var building_Bed = RestUtility.FindBedFor(victim3, pawn, true, false) ?? RestUtility.FindBedFor(victim3, pawn, true, false, true);
                        if (building_Bed == null)
                        {
                            Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim3, MessageTypeDefOf.RejectInput, false);
                        }
                        else
                        {
                            var job16 = JobMaker.MakeJob(JobDefOf.Capture, victim3, building_Bed);
                            job16.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job16);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                            if (victim3.Faction != null && victim3.Faction != Faction.OfPlayer && !victim3.Faction.def.hidden &&
                                !victim3.Faction.HostileTo(Faction.OfPlayer) && !victim3.IsPrisonerOfColony)
                                Messages.Message("MessageCapturingWillAngerFaction".Translate(victim3.Named("PAWN")).AdjustedFor(victim3), victim3,
                                    MessageTypeDefOf.CautionInput, false);
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, victim3), pawn, victim3));
                }

                foreach (var item11 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    var localTargetInfo = item11;
                    var victim2 = (Pawn) localTargetInfo.Thing;
                    if (!victim2.Downed || !pawn.CanReserveAndReach(victim2, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) ||
                        Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn, true) == null) continue;
                    
                    string text2 = "CarryToCryptosleepCasket".Translate(localTargetInfo.Thing.LabelCap, localTargetInfo.Thing);
                    var jDef = JobDefOf.CarryToCryptosleepCasket;

                    void Action2()
                    {
                        var building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn) ?? Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn, true);
                        if (building_CryptosleepCasket == null)
                        {
                            Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim2, MessageTypeDefOf.RejectInput, false);
                        }
                        else
                        {
                            var job15 = JobMaker.MakeJob(jDef, victim2, building_CryptosleepCasket);
                            job15.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job15);
                        }
                    }

                    if (!victim2.IsQuestLodger())
                    {
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, Action2, MenuOptionPriority.Default, null, victim2), pawn, victim2));
                    }
                    else
                    {
                        text2 += " (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")";
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
                    }
                }

                if (ModsConfig.RoyaltyActive)
                    foreach (var item12 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForShuttle(pawn), true))
                    {
                        var localTargetInfo2 = item12;
                        var victim = (Pawn) localTargetInfo2.Thing;

                        bool Validator(Thing thing)
                        {
                            return thing.TryGetComp<CompShuttle>()?.IsAllowed(victim) ?? false;
                        }

                        var shuttleThing = GenClosest.ClosestThingReachable(victim.Position, victim.Map, ThingRequest.ForDef(ThingDefOf.Shuttle), PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn), 9999f, Validator);
                        if (shuttleThing == null || !pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) ||
                            pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)) continue;
                        
                        string label2 = "CarryToShuttle".Translate(localTargetInfo2.Thing);

                        void Action3()
                        {
                            var compShuttle = shuttleThing.TryGetComp<CompShuttle>();
                            if (!compShuttle.LoadingInProgressOrReadyToLaunch) TransporterUtility.InitiateLoading(Gen.YieldSingle(compShuttle.Transporter));
                            var job14 = JobMaker.MakeJob(JobDefOf.HaulToTransporter, victim, shuttleThing);
                            job14.ignoreForbidden = true;
                            job14.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job14);
                        }

                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label2, Action3), pawn, victim));
                    }
            }

            opts.AddRange(GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForStrip(pawn), true)
                .Select(stripTarg => pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly)
                    ? stripTarg.Pawn == null || !stripTarg.Pawn.HasExtraHomeFaction() ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Strip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing), delegate
                    {
                        stripTarg.Thing.SetForbidden(false, false);
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, stripTarg));
                    }), pawn, stripTarg) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null)
                    : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "NoPath".Translate(), null)));

            ThingWithComps equipment = null;
            if (pawn.equipment != null)
            {
                if (target.TryGetComp<CompEquippable>() != null)
                    equipment = (ThingWithComps) target;

                if (equipment != null)
                {
                    var labelShort = equipment.LabelShort;
                    FloatMenuOption item6;
                    if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn), null);
                    }
                    else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "NoPath".Translate(), null);
                    }
                    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "Incapable".Translate(), null);
                    }
                    else if (equipment.IsBurning())
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
                    }
                    else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(equipment, pawn))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
                    }
                    else if (!EquipmentUtility.CanEquip(equipment, pawn, out var cantReason))
                    {
                        item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + cantReason.CapitalizeFirst(), null);
                    }
                    else
                    {
                        string text3 = "Equip".Translate(labelShort);
                        if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler)) text3 += " " + "EquipWarningBrawler".Translate();
                        item6 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, delegate
                        {
                            var equipWeaponConfirmationDialogText = ThingRequiringRoyalPermissionUtility.GetEquipWeaponConfirmationDialogText(equipment, pawn);
                            var compBladelinkWeapon = equipment.TryGetComp<CompBladelinkWeapon>();
                            if (compBladelinkWeapon != null && compBladelinkWeapon.bondedPawn != pawn)
                            {
                                if (!equipWeaponConfirmationDialogText.NullOrEmpty()) equipWeaponConfirmationDialogText += "\n\n";
                                equipWeaponConfirmationDialogText += "BladelinkEquipWarning".Translate();
                            }

                            if (!equipWeaponConfirmationDialogText.NullOrEmpty())
                            {
                                equipWeaponConfirmationDialogText += "\n\n" + "RoyalWeaponEquipConfirmation".Translate();
                                Find.WindowStack.Add(new Dialog_MessageBox(equipWeaponConfirmationDialogText, "Yes".Translate(), delegate { Equip(); }, "No".Translate()));
                            }
                            else
                            {
                                Equip();
                            }
                        }, MenuOptionPriority.High), pawn, equipment);
                    }

                    opts.Add(item6);
                }
            }

            Apparel apparel;
            if (pawn.apparel != null)
            {
                apparel = target as Apparel;

                if (apparel != null)
                {
                    var item7 = !pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly)
                        ? new FloatMenuOption("CannotWear".Translate(apparel.Label, apparel) + ": " + "NoPath".Translate(), null)
                        : apparel.IsBurning()
                            ? new FloatMenuOption("CannotWear".Translate(apparel.Label, apparel) + ": " + "Burning".Translate(), null)
                            : pawn.apparel.WouldReplaceLockedApparel(apparel)
                                ? new FloatMenuOption("CannotWear".Translate(apparel.Label, apparel) + ": " + "WouldReplaceLockedApparel".Translate().CapitalizeFirst(), null)
                                : !ApparelUtility.HasPartsToWear(pawn, apparel.def)
                                    ? new FloatMenuOption("CannotWear".Translate(apparel.Label, apparel) + ": " + "CannotWearBecauseOfMissingBodyParts".Translate(), null)
                                    : EquipmentUtility.CanEquip(apparel, pawn, out var cantReason2)
                                        ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ForceWear".Translate(apparel.LabelShort, apparel), delegate
                                        {
                                            apparel.SetForbidden(false);
                                            var job13 = JobMaker.MakeJob(JobDefOf.Wear, apparel);
                                            pawn.jobs.TryTakeOrderedJob(job13);
                                        }, MenuOptionPriority.High), pawn, apparel)
                                        : new FloatMenuOption("CannotWear".Translate(apparel.Label, apparel) + ": " + cantReason2, null);
                    opts.Add(item7);
                }
            }

            if (pawn.IsFormingCaravan())
            {
                var item3 = target;
                if (item3 != null && item3.def.EverHaulable)
                {
                    var packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
                    var jobDef = packTarget == pawn ? JobDefOf.TakeInventory : JobDefOf.GiveToPackAnimal;
                    if (!pawn.CanReach(item3, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item3.Label, item3) + ": " + "NoPath".Translate(), null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item3, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item3.Label, item3) + ": " + "TooHeavy".Translate(), null));
                    }
                    else
                    {
                        var lordJob = (LordJob_FormAndSendCaravan) pawn.GetLord().LordJob;
                        var capacityLeft = CaravanFormingUtility.CapacityLeft(lordJob);
                        if (item3.stackCount == 1)
                        {
                            var capacityLeft2 = capacityLeft - item3.GetStatValue(StatDefOf.Mass);
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravan".Translate(item3.Label, item3), capacityLeft2), delegate
                                {
                                    item3.SetForbidden(false, false);
                                    var job12 = JobMaker.MakeJob(jobDef, item3);
                                    job12.count = 1;
                                    job12.checkEncumbrance = packTarget == pawn;
                                    pawn.jobs.TryTakeOrderedJob(job12);
                                }, MenuOptionPriority.High), pawn, item3));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item3, item3.stackCount))
                            {
                                opts.Add(new FloatMenuOption("CannotLoadIntoCaravanAll".Translate(item3.Label, item3) + ": " + "TooHeavy".Translate(), null));
                            }
                            else
                            {
                                var capacityLeft3 = capacityLeft - item3.stackCount * item3.GetStatValue(StatDefOf.Mass);
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                                    CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanAll".Translate(item3.Label, item3), capacityLeft3), delegate
                                    {
                                        item3.SetForbidden(false, false);
                                        var job11 = JobMaker.MakeJob(jobDef, item3);
                                        job11.count = item3.stackCount;
                                        job11.checkEncumbrance = packTarget == pawn;
                                        pawn.jobs.TryTakeOrderedJob(job11);
                                    }, MenuOptionPriority.High), pawn, item3));
                            }

                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("LoadIntoCaravanSome".Translate(item3.LabelNoCount, item3), delegate
                            {
                                var to3 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, item3), item3.stackCount);
                                var window3 = new Dialog_Slider(delegate(int val)
                                {
                                    var capacityLeft4 = capacityLeft - val * item3.GetStatValue(StatDefOf.Mass);
                                    return CaravanFormingUtility.AppendOverweightInfo(string.Format("LoadIntoCaravanCount".Translate(item3.LabelNoCount, item3), val),
                                        capacityLeft4);
                                }, 1, to3, delegate(int count)
                                {
                                    item3.SetForbidden(false, false);
                                    var job10 = JobMaker.MakeJob(jobDef, item3);
                                    job10.count = count;
                                    job10.checkEncumbrance = packTarget == pawn;
                                    pawn.jobs.TryTakeOrderedJob(job10);
                                });
                                Find.WindowStack.Add(window3);
                            }, MenuOptionPriority.High), pawn, item3));
                        }
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
            {
                var item2 = target;
                if (item2 != null && item2.def.EverHaulable)
                {
                    if (!pawn.CanReach(item2, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(item2.Label, item2) + ": " + "NoPath".Translate(), null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item2, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(item2.Label, item2) + ": " + "TooHeavy".Translate(), null));
                    }
                    else if (item2.stackCount == 1)
                    {
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUp".Translate(item2.Label, item2), delegate
                        {
                            item2.SetForbidden(false, false);
                            var job9 = JobMaker.MakeJob(JobDefOf.TakeInventory, item2);
                            job9.count = 1;
                            job9.checkEncumbrance = true;
                            pawn.jobs.TryTakeOrderedJob(job9);
                        }, MenuOptionPriority.High), pawn, item2));
                    }
                    else
                    {
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item2, item2.stackCount))
                            opts.Add(new FloatMenuOption("CannotPickUpAll".Translate(item2.Label, item2) + ": " + "TooHeavy".Translate(), null));
                        else
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpAll".Translate(item2.Label, item2), delegate
                            {
                                item2.SetForbidden(false, false);
                                var job8 = JobMaker.MakeJob(JobDefOf.TakeInventory, item2);
                                job8.count = item2.stackCount;
                                job8.checkEncumbrance = true;
                                pawn.jobs.TryTakeOrderedJob(job8);
                            }, MenuOptionPriority.High), pawn, item2));
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpSome".Translate(item2.LabelNoCount, item2), delegate
                        {
                            var to2 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item2), item2.stackCount);
                            var window2 = new Dialog_Slider("PickUpCount".Translate(item2.LabelNoCount, item2), 1, to2, delegate(int count)
                            {
                                item2.SetForbidden(false, false);
                                var job7 = JobMaker.MakeJob(JobDefOf.TakeInventory, item2);
                                job7.count = count;
                                job7.checkEncumbrance = true;
                                pawn.jobs.TryTakeOrderedJob(job7);
                            });
                            Find.WindowStack.Add(window2);
                        }, MenuOptionPriority.High), pawn, item2));
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
            {
                var item = target;
                if (item != null && item.def.EverHaulable)
                {
                    var bestPackAnimal = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn);
                    if (bestPackAnimal != null)
                    {
                        if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item.Label, item) + ": " + "NoPath".Translate(), null));
                        }
                        else if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, 1))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                        }
                        else if (item.stackCount == 1)
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimal".Translate(item.Label, item), delegate
                            {
                                item.SetForbidden(false, false);
                                var job6 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                job6.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job6);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, item.stackCount))
                                opts.Add(new FloatMenuOption("CannotGiveToPackAnimalAll".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                            else
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalAll".Translate(item.Label, item), delegate
                                {
                                    item.SetForbidden(false, false);
                                    var job5 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                    job5.count = item.stackCount;
                                    pawn.jobs.TryTakeOrderedJob(job5);
                                }, MenuOptionPriority.High), pawn, item));
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalSome".Translate(item.LabelNoCount, item), delegate
                            {
                                var to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(bestPackAnimal, item), item.stackCount);
                                var window = new Dialog_Slider("GiveToPackAnimalCount".Translate(item.LabelNoCount, item), 1, to, delegate(int count)
                                {
                                    item.SetForbidden(false, false);
                                    var job4 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                    job4.count = count;
                                    pawn.jobs.TryTakeOrderedJob(job4);
                                });
                                Find.WindowStack.Add(window);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                    }
                }
            }

            if (!pawn.Map.IsPlayerHome && pawn.Map.exitMapGrid.MapUsesExitGrid)
                foreach (var item14 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    var p = (Pawn) item14.Thing;
                    if (p.Faction != Faction.OfPlayer && !p.IsPrisonerOfColony && !CaravanUtility.ShouldAutoCapture(p, Faction.OfPlayer)) continue;
                    
                    if (!pawn.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate(), null));
                    }
                    else if (!RCellFinder.TryFindBestExitSpot(pawn, out var exitSpot))
                    {
                        opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate(), null));
                    }
                    else
                    {
                        var taggedString2 = p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony
                            ? "CarryToExit".Translate(p.Label, p)
                            : "CarryToExitAndCapture".Translate(p.Label, p);
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString2, delegate
                        {
                            var job3 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToExit, p, exitSpot);
                            job3.count = 1;
                            job3.failIfCantJoinOrCreateCaravan = true;
                            pawn.jobs.TryTakeOrderedJob(job3);
                        }, MenuOptionPriority.High), pawn, item14));
                    }
                }

            if (pawn.equipment?.Primary != null && GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForSelf(pawn), true).Any())
            {
                if (pawn.IsQuestLodger())
                {
                    opts.Add(new FloatMenuOption("CannotDrop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary) + ": " + "QuestRelated".Translate().CapitalizeFirst(),
                        null));
                }
                else
                {
                    void Action4()
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, pawn.equipment.Primary));
                    }

                    opts.Add(new FloatMenuOption("Drop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary), Action4, MenuOptionPriority.Default, null, pawn));
                }
            }

            foreach (var item15 in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForTrade(), true))
                if (!pawn.CanReach(item15, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + ": " + "NoPath".Translate(), null));
                }
                else if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
                {
                    opts.Add(new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null));
                }
                else if (!pawn.CanTradeWith(((Pawn) item15.Thing).Faction, ((Pawn) item15.Thing).TraderKind))
                {
                    opts.Add(new FloatMenuOption("CannotTradeMissingTitleAbility".Translate(), null));
                }
                else
                {
                    var pTarg = (Pawn) item15.Thing;

                    void Action5()
                    {
                        var job2 = JobMaker.MakeJob(JobDefOf.TradeWithPawn, pTarg);
                        job2.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job2);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                    }

                    var t2 = "";
                    if (pTarg.Faction != null) t2 = " (" + pTarg.Faction.Name + ")";
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption("TradeWith".Translate(pTarg.LabelShort + ", " + pTarg.TraderKind.label) + t2, Action5, MenuOptionPriority.InitiateSocial, null,
                            item15.Thing), pawn, pTarg));
                }

            foreach (var casket in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForOpen(pawn), true))
                if (!pawn.CanReach(casket, PathEndMode.OnCell, Danger.Deadly))
                    opts.Add(new FloatMenuOption("CannotOpen".Translate(casket.Thing) + ": " + "NoPath".Translate(), null));
                else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    opts.Add(new FloatMenuOption("CannotOpen".Translate(casket.Thing) + ": " + "Incapable".Translate(), null));
                else if (casket.Thing.Map.designationManager.DesignationOn(casket.Thing, DesignationDefOf.Open) == null)
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Open".Translate(casket.Thing), delegate
                    {
                        var job = JobMaker.MakeJob(JobDefOf.Open, casket.Thing);
                        job.ignoreDesignations = true;
                        pawn.jobs.TryTakeOrderedJob(job);
                    }, MenuOptionPriority.High), pawn, casket.Thing));
            opts.AddRange(target.GetFloatMenuOptions(pawn));

            void Equip()
            {
                equipment.SetForbidden(false);
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, equipment));
                MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
            }
        }
    }
}