using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class Settings : ModSettings
    {
        // Architect Menu:
        // The defName for the DesignationCategoryDef the mod items are in by default:
        //TODO: make this a tutorial, provide link.
        // To use this code in another mod, change this const string, and then add to the
        // file ModName/Languages/English(etc)/Keyed/settings(or whatever).xml:
        // <[const string]_ArchitectMenuSettings>Location on Architect Menu:</...>
        // Copy and paste the rest of anything that says "Architect Menu"
        // Change the list of new mod items in the final place "Architect Menu" tells you to
        private const string architectMenuDefaultDesigCatDef = "LWM_DS_Storage";
        public static bool robotsCanUse;
        public static bool storingTakesTime = true;
        public static float storingGlobalScale = 1f;
        public static bool storingTimeConsidersStackSize = true;
        public static StoragePriority defaultStoragePriority = StoragePriority.Important;

        public static bool useDeepStorageRightClickLogic;
        
        public static bool useDeepStorageNewRightClick;
        public static float newNRC_IconScaling = 1f;
        public static string newNRC_IconScalingBuff;
        public static float newNRC_BoxHeight = 32f;
        public static string newNRC_BoxHeightBuff;
        
        // public static bool duseDeepStorageNewRightClick;

        // Turning this off removes conflicts with some other storage mods (at least I hope so):
        //   (RimFactory? I think?)
        public static bool checkOverCapacity = true;

        public static bool allowPerDSUSettings;
        private static string architectMenuDesigCatDef = architectMenuDefaultDesigCatDef;
        private static bool architectMenuAlwaysShowCategory;

        private static bool architectMenuMoveALLStorageItems = true;

        //   For later use if def is removed from menu...so we can put it back:
        private static DesignationCategoryDef architectMenuActualDef;
        private static bool architectMenuAlwaysShowTmp;
        private static bool architectMenuMoveALLTmp = true;


        public static List<ThingDef> allDeepStorageUnits;

        private static Vector2 scrollPosition = new Vector2(0f, 0f);
        private static Rect viewRect = new Rect(0, 0, 100f, 10000f); // OMG OMG OMG I got scrollView in Listing_Standard to work!

        public static void DoSettingsWindowContents(Rect inRect)
        {
            var outerRect = inRect.ContractedBy(10f);
            Widgets.DrawHighlight(outerRect);
            var l = new Listing_Standard(GameFont.Medium); // my tiny high-resolution monitor :p
            l.BeginScrollView(outerRect, ref scrollPosition, ref viewRect);

            //l.GapLine();  // Who can haul to Deep Storage (robots, animals, etc)
            l.CheckboxLabeled("LWMDSrobotsCanUse".Translate(), ref robotsCanUse, "LWMDSrobotsCanUseDesc".Translate());
            string[] intLabels =
            {
                "LWM_DS_Int_Animal".Translate(),
                "LWM_DS_Int_ToolUser".Translate(),
                "LWM_DS_Int_Humanlike".Translate()
            };
            // Setting to allow bionic racoons to haul to Deep Storage:
            l.EnumRadioButton(ref Patch_IsGoodStoreCell.NecessaryIntelligenceToUseDeepStorage, "LWM_DS_IntTitle".Translate(),
                "LWM_DS_IntDesc".Translate(), false, intLabels);
            l.GapLine(); //Storing Delay Settings
            l.Label("LWMDSstoringDelaySettings".Translate());
            l.CheckboxLabeled("LWMDSstoringTakesTimeLabel".Translate(),
                ref storingTakesTime, "LWMDSstoringTakesTimeDesc".Translate());
            l.Label("LWMDSstoringGlobalScale".Translate((storingGlobalScale * 100f).ToString("0.")));
            storingGlobalScale = l.Slider(storingGlobalScale, 0f, 2f);
            l.CheckboxLabeled("LWMDSstoringTimeConsidersStackSize".Translate(),
                ref storingTimeConsidersStackSize, "LWMDSstoringTimeConsidersStackSizeDesc".Translate());
            // Reset storing delay settings to defaults
            if (l.ButtonText("LWMDSstoringDelaySettings".Translate() + ": " + "ResetBinding".Translate() /*Reset to Default*/))
            {
                storingTakesTime = true;
                storingGlobalScale = 1f;
                storingTimeConsidersStackSize = true;
            }

            l.GapLine(); // default Storing Priority
            if (l.ButtonTextLabeled("LWM_DS_defaultStoragePriority".Translate(),
                defaultStoragePriority.Label()))
            {
                var mlist = (from StoragePriority p in Enum.GetValues(typeof(StoragePriority))
                    select new FloatMenuOption(p.Label(), delegate
                    {
                        defaultStoragePriority = p;
                        foreach (var d in allDeepStorageUnits) d.building.defaultStorageSettings.Priority = p;
                    })).ToList();
                Find.WindowStack.Add(new FloatMenu(mlist));
            }

            l.GapLine();
            l.Label("LWM_DS_userInterface".Translate());
            l.CheckboxLabeled("LWM_DS_useDSRightClick".Translate(), ref useDeepStorageRightClickLogic,
                "LWM_DS_useDSRightClickDesc".Translate());

            l.Gap();
            // TODO: WIP
            l.CheckboxLabeled("LWM_DS_useNewRightClick".Translate(), ref useDeepStorageNewRightClick,
                "LWM_DS_useNewRightClickDesc".Translate());
            
            if (useDeepStorageNewRightClick)
            {
                l.TextFieldNumericLabeled("LWM_DS_newNRC_IconScaling".Translate(), ref newNRC_IconScaling, ref newNRC_IconScalingBuff, 0.1f);
                l.TextFieldNumericLabeled("LWM_DS_newNRC_BoxHeight".Translate(), ref newNRC_BoxHeight, ref newNRC_BoxHeightBuff, 10f);
            }

            // Architect Menu:
            l.GapLine(); //Architect Menu location
/*
//            string archLabel=
//            if (archLabel==n
//            l.Label("LWMDSarchitectMenuSettings".Translate());
            if (architectMenuDesigCatDef==architectMenuDefaultDesigCatDef) {
//                if (architectLWM_DS_Storage_DesignationCatDef==null) {
//                    Log.Error("LWM.DeepStorage: architectLWM_DS_Storage_DesignationCatDef was null; this should never happen.");
//                    tmp="ERROR";
//                } else {
//                    tmp=architectCurrentDesignationCatDef.LabelCap; // todo: (default)
//                }
                archLabel+=" ("+
            } else {
                var x=DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDesignationCatDefDefName, false);
                if (x==null) {
                    // TODO
                }
                tmp=x.LabelCap; // todo: (<menuname>)
            }*/
            if (l.ButtonTextLabeled((architectMenuDefaultDesigCatDef + "_ArchitectMenuSettings").Translate(), // Label
                // value of dropdown button:
                DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDesigCatDef)?.LabelCap
                ?? "--ERROR--"))
            {
                // error display text
//                                     , DefDatabase<DesigarchitectMenuDesigCatDef) ) {
                // Float menu for architect Menu choice:
                var alist = new List<FloatMenuOption>();
                var arl = DefDatabase<DesignationCategoryDef>.AllDefsListForReading; //all reading list
                //oops:
//                alist.Add(new FloatMenuOption(DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDefaultDesigCatDef).LabelCap
                alist.Add(new FloatMenuOption(architectMenuActualDef.LabelCap +
                                              " (" + "default".Translate() + " - " + architectMenuActualDef.defName + ")",
                    delegate
                    {
                        Utils.Mess(Utils.DBF.Settings, "Architect Menu placement set to default Storage");
                        ArchitectMenu_ChangeLocation(architectMenuDefaultDesigCatDef);
//                                                  architectCurrentDesignationCatDef=architectLWM_DS_Storage_DesignationCatDef;
//                                                  architectMenuDesignationCatDefDefName="LWM_DS_Storage";
//
//                                                  SettingsChanged();
                    }));
                // Architect Menu:  You may remove the "Furniture" references here if you wish
                alist.Add(new FloatMenuOption(DefDatabase<DesignationCategoryDef>.GetNamed("Furniture").LabelCap +
                                              " (Furniture)", // I know what this one's defName is!
                    delegate
                    {
                        Utils.Mess(Utils.DBF.Settings, "Architect Menu placement set to Furniture.");
                        ArchitectMenu_ChangeLocation("Furniture");
                    }));
                alist.AddRange(from adcd in arl
                    where adcd.defName != architectMenuDefaultDesigCatDef && adcd.defName != "Furniture"
                    select new FloatMenuOption(adcd.LabelCap + " (" + adcd.defName + ")", delegate
                    {
                        Utils.Mess(Utils.DBF.Settings, "Architect Menu placement set to " + adcd);
                        ArchitectMenu_ChangeLocation(adcd.defName);
                    }));
                Find.WindowStack.Add(new FloatMenu(alist));
            }

            l.CheckboxLabeled((architectMenuDefaultDesigCatDef + "_ArchitectMenuAlwaysShowCategory").Translate(),
                ref architectMenuAlwaysShowCategory,
                (architectMenuDefaultDesigCatDef + "_ArchitectMenuAlwaysShowDesc").Translate());
            // Do we always display?  If so, display:
            if (architectMenuAlwaysShowCategory != architectMenuAlwaysShowTmp)
            {
                if (architectMenuAlwaysShowCategory)
                    ArchitectMenu_Show();
                else if (architectMenuDesigCatDef != architectMenuDefaultDesigCatDef) ArchitectMenu_Hide();
                architectMenuAlwaysShowTmp = architectMenuAlwaysShowCategory;
            }

            l.CheckboxLabeled((architectMenuDefaultDesigCatDef + "_ArchitectMenuMoveALL").Translate(),
                ref architectMenuMoveALLStorageItems,
                (architectMenuDefaultDesigCatDef + "_ArchitectMenuMoveALLDesc").Translate());
            if (architectMenuMoveALLStorageItems != architectMenuMoveALLTmp)
            {
                //  If turning off "all things in Storage", make sure to
                //    dump all the items into Furniture, to make sure they
                //    can at least be found somewhere.
                var ctmp = architectMenuDesigCatDef;
                if (architectMenuMoveALLStorageItems == false)
                {
                    architectMenuMoveALLStorageItems = true;
                    ArchitectMenu_ChangeLocation("Furniture");
                    architectMenuMoveALLStorageItems = false;
                }

                ArchitectMenu_ChangeLocation(ctmp);
                architectMenuMoveALLTmp = architectMenuMoveALLStorageItems;
            }

            // finished drawing settings for Architect Menu
            // -------------------
            // Allow player to turn of Over-Capacity check.
            //   Turn it off automatically for Project RimFactory and Extended Storage
            //   Note: should turn it off automatically for any other storage mods, too
            l.GapLine();
            var origColor = GUI.color; // make option gray if ignored
            var tmpMod = ModLister.GetActiveModWithIdentifier("spdskatr.projectrimfactory");
            if (tmpMod != null)
            {
                GUI.color = Color.gray;
                // This setting is disabled due to mod Extended Storage
                l.Label("LWMDSignoredDueTo".Translate(tmpMod.Name));
            }

            if ((tmpMod = ModLister.GetActiveModWithIdentifier("Skullywag.ExtendedStorage")) != null)
            {
                GUI.color = Color.gray;
                l.Label("LWMDSignoredDueTo".Translate(tmpMod.Name));
            }

            l.CheckboxLabeled("LWMDSoverCapacityCheck".Translate(), ref checkOverCapacity,
                "LWMDSoverCapacityCheckDesc".Translate());
            GUI.color = origColor;
            // Per DSU settings - let players change them around...
            l.GapLine();
            if (allowPerDSUSettings)
            {
                if (l.ButtonText("LWMDSperDSUSettings".Translate())) Find.WindowStack.Add(new Dialog_DS_Settings());
            }
            else
            {
                l.CheckboxLabeled("LWMDSperDSUturnOn".Translate(), ref allowPerDSUSettings,
                    "LWMDSperDSUturnOnDesc".Translate());
            }

            l.GapLine(); // End. Finis. Looks pretty having a line at the end.
            l.EndScrollView(ref viewRect);
        }

        public static void DefsLoaded()
        {
//            Log.Warning("LWM.deepstorag - defs loaded");
            // Todo? If settings are different from defaults, then:
            Setup();
            // Architect Menu:
            if (architectMenuDesigCatDef != architectMenuDefaultDesigCatDef ||
                architectMenuMoveALLStorageItems) // in which case, we need to redo menu anyway
                ArchitectMenu_ChangeLocation(architectMenuDesigCatDef, true);
            // Other def-related changes:
            if (defaultStoragePriority != StoragePriority.Important)
                foreach (var d in allDeepStorageUnits)
                    d.building.defaultStorageSettings.Priority = defaultStoragePriority;
            // Re-read Mod Settings - some won't have been read because Defs weren't loaded:
            //   (do this after above to allow user to override changes)
            //   (LoadedModManager.GetMod(typeof(DeepStorageMod)).Content.Identifier and typeof(DeepStorageMod).Name by the way)
//todo:
            Utils.Mess(Utils.DBF.Settings, "Defs Loaded.  About to re-load settings");
            //var s = LoadedModManager.ReadModSettings<Settings>("LWM.DeepStorage", "DeepStorageMod");
            var mod = LoadedModManager.GetMod(typeof(DeepStorageMod));
            var s = LoadedModManager.ReadModSettings<Settings>(mod.Content.FolderName, "DeepStorageMod");
        }

        // Architect Menu:
        public static void ArchitectMenu_ChangeLocation(string newDefName, bool loadingOnStartup = false)
        {
//          Utils.Warn(Utils.DBF.Settings, "SettingsChanged()");
            var prevDesignationCatDef = loadingOnStartup
                ? DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDefaultDesigCatDef)
                : DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDesigCatDef, false);
            // If switching to default, put default into def database.
            if (newDefName == architectMenuDefaultDesigCatDef) ArchitectMenu_Show();
            // Compatibility Logic:
            //   If certain mods are loaded and all storage units are to go in one menu,
            //   maybe we want to remove the other menu?  Or maybe we want to use that
            //   one by default:
            // For Deep Storage, if the player also has Quantum Storage, use their menu insead:
            if (architectMenuMoveALLStorageItems && !architectMenuAlwaysShowCategory &&
                newDefName == architectMenuDefaultDesigCatDef &&
                ModLister.GetActiveModWithIdentifier("Cheetah.QuantumStorageRedux") != null)
                newDefName = "QSRStorage";
            var newDesignationCatDef = DefDatabase<DesignationCategoryDef>.GetNamed(newDefName);
            if (newDesignationCatDef == null)
            {
                Log.Warning("LWM.DeepStorage: Failed to find menu category " + newDefName + " - reverting to default");
                newDefName = architectMenuDefaultDesigCatDef;
                ArchitectMenu_Show();
                newDesignationCatDef = DefDatabase<DesignationCategoryDef>.GetNamed(newDefName);
            }

            // Architect Menu: Specify all your buildings/etc:
            //   var allMyBuildings=DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x=>x.HasComp(etc)));
            var itemsToMove = allDeepStorageUnits;
            Utils.Mess(Utils.DBF.Settings, "Moving these units to 'Storage' menu: " + string.Join(", ", itemsToMove));
            // We can move ALL the storage buildings!  If the player wants.  I do.
            if (architectMenuMoveALLStorageItems)
            {
//                Log.Error("Trying to mvoe everythign:");
                var desigProduction = DefDatabase<DesignationCategoryDef>.GetNamed("Production");
                // Interesting detail: apparently it IS possible to have thingDefs with null thingClass... weird.
                itemsToMove = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x?.thingClass != null && (x.thingClass == typeof(Building_Storage) ||
                                                                                                                 x.thingClass.IsSubclassOf(typeof(Building_Storage)))
                                                                                                             && x.designationCategory != desigProduction);
                // testing:
//                itemsToMove.AddRange(DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x=>x.defName.Contains("MURWallLight")));
            }

            var _resolvedDesignatorsField = typeof(DesignationCategoryDef)
                .GetField("resolvedDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var d in itemsToMove.Where(d => d.designationCategory != null))
            {
                //                Log.Error("Moving item "+d.defName+" (category: "+(d.designationCategory!=null?d.designationCategory.ToString():"NONE"));
                if (_resolvedDesignatorsField != null)
                {
                    var resolvedDesignators = (List<Designator>) _resolvedDesignatorsField.GetValue(d.designationCategory);
                    if (d.designatorDropdown == null)
                    {
//                    Log.Message("No dropdown");
                        // easy case:
//                    Log.Message("  Removed this many entries in "+d.designationCategory+": "+
                        resolvedDesignators.RemoveAll(x => x is Designator_Build &&
                                                           ((Designator_Build) x).PlacingDef == d);
//                        );
                        // Now do new:
                        resolvedDesignators = (List<Designator>) _resolvedDesignatorsField.GetValue(newDesignationCatDef);
                        // To make sure there are no duplicates:
                        resolvedDesignators.RemoveAll(x => x is Designator_Build &&
                                                           ((Designator_Build) x).PlacingDef == d);
                        resolvedDesignators.Add(new Designator_Build(d));
                    }
                    else
                    {
//                    Log.Warning("LWM.DeepStorage: ThingDef "+d.defName+" has a dropdown Designator.");
                        // Hard case: Designator_Dropdowns!
                        var dd = (Designator_Dropdown) resolvedDesignators.Find(x => x is Designator_Dropdown &&
                                                                                     ((Designator_Dropdown) x).Elements
                                                                                     .Find(y => y is Designator_Build &&
                                                                                                ((Designator_Build) y).PlacingDef == d) != null);
                        if (dd != null)
                        {
//                        Log.Message("Found dropdown designator for "+d.defName);
                            resolvedDesignators.Remove(dd);
                            // Switch to new category:
                            resolvedDesignators = (List<Designator>) _resolvedDesignatorsField.GetValue(newDesignationCatDef);
                            if (!resolvedDesignators.Contains(dd)) //                            Log.Message("  Adding to new category "+newDesignationCatDef);
                                resolvedDesignators.Add(dd);
//                    } else { //debug
//                        Log.Message("   ThingDef "+d.defName+" has designator_dropdown "+d.designatorDropdown.defName+
//                            ", but cannot find it in "+d.designationCategory+" - this is okay if something else added it.");
                        }
                    }
                }

                d.designationCategory = newDesignationCatDef;
            }
            // Flush designation category defs:.....dammit
//            foreach (var x in DefDatabase<DesignationCategoryDef>.AllDefs) {
//                x.ResolveReferences();
//            }
//            prevDesignationCatDef?.ResolveReferences();
//            newDesignationCatDef.ResolveReferences();
            //ArchitectMenu_ClearCache(); // we do this later one way or another

            // To remove the mod's DesignationCategoryDef from Architect menu:
            //   remove it from RimWorld.MainTabWindow_Architect's desPanelsCached.
            // To do that, we remove it from the DefDatabase and then rebuild the cache.
            //   Removing only the desPanelsCached entry does work: the entry is
            //   recreated when a new game is started.  So if the options are changed
            //   and then a new game started...the change gets lost.
            // So we have to update the DefsDatabase.
            // This is potentially difficult: the .index can get changed, and that
            //   can cause problems.  But nothing seems to use the .index for any
            //   DesignationCategoryDef except for the menu, so manually adjusting
            //   the DefsDatabase is safe enough:
            if (!architectMenuAlwaysShowCategory && newDefName != architectMenuDefaultDesigCatDef) ArchitectMenu_Hide();
            // ArchitectMenu_ClearCache(); //hide flushes cache
//                    if (tmp.AllResolvedDesignators.Count <= tmp.specialDesignatorClasses.Count)
//                        isCategoryEmpty=false;
/*
//                    Log.Message("Removing old menu!");
                    // DefDatabase<DesignationCategoryDef>.Remove(tmp);
                    if (!tmp.AllResolvedDesignators.NullOrEmpty()) {
                        foreach (var d in tmp.AllResolvedDesignators) {
                            if (!tmp.specialDesignatorClasses.Contains(d)) {
                                isCategoryEmpty=false;
                                break;
                            }
                        }
                    }
                    */
//                    if (isCategoryEmpty)
            else // Simply flush cache:
                ArchitectMenu_ClearCache();
            // Note that this is not perfect: if the default menu was already open, it will still be open (and
            //   empty) when the settings windows are closed.  Whatever.


            // Oh, and actually change the setting that's stored:
            architectMenuDesigCatDef = newDefName;

/*            List<ArchitectCategoryTab> archMenu=(List<ArchitectCategoryTab>)Harmony.AccessTools
                .Field(typeof(RimWorld.MainTabWindow_Architect), "desPanelsCached")
                .GetValue((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow);
            archMenu.RemoveAll(t=>t.def.defName==architectMenuDefaultDesigCatDef);

            archMenu.Add(new ArchitectCategoryTab(newDesignationCatDef));
            archMenu.Sort((a,b)=>a.def.order.CompareTo(b.def.order));
            archMenu.SortBy(a=>a.def.order, b=>b.def.order); // May need (type of var a)=>...

            */


/*            Harmony.AccessTools.Method(typeof(RimWorld.MainTabWindow_Architect), "CacheDesPanels")
                .Invoke(((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow), null);*/


/*

            if (architectMenuDesignationCatDefDefName=="LWM_DS_Storage") { // default
                if (DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("LWM_DS_Storage") == null) {
                    Utils.Mess(Utils.DBF.Settings,"Adding 'Storage' to the architect menu.");
                    DefDatabase<DesignationCategoryDef>.Add(architectLWM_DS_Storage_DesignationCatDef);
                } else {
                    Utils.Mess(Utils.DBF.Settings, "No need to add 'Storage' to the architect menu.");
                }
                architectCurrentDesignationCatDef=architectLWM_DS_Storage_DesignationCatDef;
            } else {
                // remove our "Storage" from the architect menu:
                Utils.Mess(Utils.DBF.Settings,"Removing 'Storage' from the architect menu.");
                DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Remove(architectLWM_DS_Storage_DesignationCatDef);
                if (DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("LWM_DS_Storage") != null) {
                    Log.Error("Failed to remove LWM_DS_Storage :("+DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("LWM_DS_Storage"));
                }

                architectCurrentDesignationCatDef=DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDesignationCatDefDefName);
            }
            prevDesignationCatDef?.ResolveReferences();
            architectCurrentDesignationCatDef.ResolveReferences();

            Harmony.AccessTools.Method(typeof(RimWorld.MainTabWindow_Architect), "CacheDesPanels")
                .Invoke((), null);
*/
            Utils.Warn(Utils.DBF.Settings, "Settings changed architect menu");
        }

        public static void ArchitectMenu_Hide()
        {
            DesignationCategoryDef tmp;
            if ((tmp = DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDefaultDesigCatDef, false)) != null
                && !architectMenuAlwaysShowCategory) // DefDatabase<DesignationCategoryDef>.Remove(tmp);
                typeof(DefDatabase<>).MakeGenericType(typeof(DesignationCategoryDef))
                    .GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] {tmp});
            // No need to SetIndices() or anything: .index are not used for DesignationCategoryDef(s).  I hope.
            ArchitectMenu_ClearCache();
        }

        public static void ArchitectMenu_Show()
        {
            if (DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDefaultDesigCatDef, false) == null) DefDatabase<DesignationCategoryDef>.Add(architectMenuActualDef);
            ArchitectMenu_ClearCache();
        }

        public static void ArchitectMenu_ClearCache()
        {
            // Clear the architect menu cache:
            //   Run the main Architect.TabWindow.CacheDesPanels()
            typeof(MainTabWindow_Architect).GetMethod("CacheDesPanels", BindingFlags.NonPublic |
                                                                        BindingFlags.Instance)
                ?.Invoke((MainTabWindow_Architect) MainButtonDefOf.Architect.TabWindow, null);
        }


        // Setup stuff that needs to be run before settings can be used.
        //   I don't risk using a static constructor because I must make sure defs have been finished loading.
        //     (testing shows this is VERY correct!!)
        //   There's probably some rimworld annotation that I could use, but this works:
        private static void Setup()
        {
            if (architectMenuActualDef == null) //Log.Message("LWM.DeepStorage Settings Setup() called first time");
                architectMenuActualDef = DefDatabase<DesignationCategoryDef>.GetNamed(architectMenuDefaultDesigCatDef);
            if (!allDeepStorageUnits.NullOrEmpty()) return;

            allDeepStorageUnits = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.HasComp(typeof(CompDeepStorage)));
            Utils.Mess(Utils.DBF.Settings, "  allDeepStorageUnits initialized: " + allDeepStorageUnits.Count + " units");

            /*           if (architectLWM_DS_Storage_DesignationCatDef==null) {
                architectLWM_DS_Storage_DesignationCatDef=DefDatabase<DesignationCategoryDef>.GetNamed("LWM_DS_Storage");
                Utils.Mess(Utils.DBF.Settings, "  Designation Category Def loaded: "+architectLWM_DS_Storage_DesignationCatDef);
            }*/
        }

        public override void ExposeData()
        {
            Utils.Warn(Utils.DBF.Settings, "Expose Data called: Mode: " + Scribe.mode);
            //Log.Error("LWM.DeepStorage: Settings ExposeData() called");
            base.ExposeData();

            Scribe_Values.Look(ref storingTakesTime, "storing_takes_time", true);
            Scribe_Values.Look(ref storingGlobalScale, "storing_global_scale", 1f);
            Scribe_Values.Look(ref storingTimeConsidersStackSize, "storing_time_CSS", true);
            Scribe_Values.Look(ref robotsCanUse, "robotsCanUse", true);
            Scribe_Values.Look(ref Patch_IsGoodStoreCell.NecessaryIntelligenceToUseDeepStorage, "int_to_use_DS", Intelligence.Humanlike);
            Scribe_Values.Look(ref defaultStoragePriority, "default_s_priority", StoragePriority.Important);
            Scribe_Values.Look(ref checkOverCapacity, "check_over_capacity", true);
            Scribe_Values.Look(ref useDeepStorageRightClickLogic, "DS_AHlO");
            Scribe_Values.Look(ref useDeepStorageNewRightClick, "DS_NRC");
            Scribe_Values.Look(ref newNRC_BoxHeight, "DS_NRCBH");
            Scribe_Values.Look(ref newNRC_IconScaling, "DS_NRCIS");
            // Architect Menu:
            Scribe_Values.Look(ref architectMenuDesigCatDef, "architect_desig", architectMenuDefaultDesigCatDef);
            Scribe_Values.Look(ref architectMenuAlwaysShowCategory, "architect_show");
            Scribe_Values.Look(ref architectMenuMoveALLStorageItems, "architect_moveall", true);
            // Per DSU Building storage settings:
            Scribe_Values.Look(ref allowPerDSUSettings, "allowPerDSUSettings");
            if (allowPerDSUSettings && !allDeepStorageUnits.NullOrEmpty()) Dialog_DS_Settings.ExposeDSUSettings(allDeepStorageUnits);
        } // end ExposeData()
    }

    internal static class DisplayHelperFunctions
    {
        // Helper function to create EnumRadioButton for Enums in settings
        public static bool EnumRadioButton<T>(this Listing_Standard ls, ref T val, string label, string tooltip = "",
            bool showEnumValues = true, string[] buttonLabels = null)
        {
            if (!(val is Enum))
            {
                Log.Error("LWM.DisplayHelperFunction: EnumRadioButton passed non-enum value");
                return false;
            }

            var result = false;
            if (tooltip == "")
                ls.Label(label);
            else
                ls.Label(label, -1, tooltip);
            var enumValues = Enum.GetValues(val.GetType());
            var i = 0;
            foreach (T x in enumValues)
            {
                string optionLabel;
                if (showEnumValues || buttonLabels == null)
                {
                    optionLabel = x.ToString();
                    if (buttonLabels != null) optionLabel += ": " + buttonLabels[i];
                }
                else
                {
                    optionLabel = buttonLabels[i]; // got a better way?
                }

                if (ls.RadioButton_NewTemp(optionLabel, val.ToString() == x.ToString()))
                {
                    val = x; // I swear....ToString() was the only thing that worked.
                    result = true;
                } // nice try, C#, nice try.

                // ((val as Enum)==(x as Enum)) // nope
                // (System.Convert.ChangeType(val, Enum.GetUnderlyingType(val.GetType()))==
                //  System.Convert.ChangeType(  x, Enum.GetUnderlyingType(  x.GetType()))) // nope
                // (x.ToString()==val.ToString())// YES!
                i++;
            }

            return result;
        }
    } // end DisplayHelperFunctions
}