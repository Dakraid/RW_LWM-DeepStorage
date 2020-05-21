using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

//using Harmony;
// for OpCodes in Harmony Transpiler

// trace utils

namespace LWM.DeepStorage
{
    public class Properties : CompProperties
    {
        /************************* Stat window (information window) ***********************/
        // This will hopefully reduce the number of annoying questions in the discussion thread
        //   "What does this store?  Can I put X in there?"
        private static StatCategoryDef DeepStorageCategory;
        public int additionalTimeEachDef = 0; // extra time to store for each different type of object there
        public int additionalTimeEachStack = 0; // extra time to store for each stack already there
        public float additionalTimeStackSize = 0f; // item with stack size 75 may take longer to store
        public StatDef altStat = null;

        private string categoriesString; // for the Stats window (information window)
        private string defsString;
        private string disallowedString;
        public float maxMassOfStoredItem = 0f;
        public int maxNumberStacks = 2;
        public float maxTotalMass = 0f;


        public int minNumberStacks = 1;
        public int minTimeStoringTakes = -1;
        public GuiOverlayType overlayType = GuiOverlayType.Normal;
        public ThingDef parent; // :p  Have to keep track of this myself
        public List<ThingDef> quickStoringItems = null;
        public bool showContents = true;

        public int size;
        public int timeStoringTakes = 1000; // measured in ticks

        public Properties()
        {
            compClass = typeof(CompDeepStorage);
        }

        public string AllowedCategoriesString
        {
            get
            {
                if (categoriesString == null)
                {
                    categoriesString = "";
                    var tf = parent?.building?.fixedStorageSettings?.filter;
                    if (tf == null) // filters can be null, e.g., shelves
                        //Log.Warning("LWM.DeepStorage:could not find filter for "+parent.defName);
                        return "";
                    var c = (List<string>) AccessTools.Field(typeof(ThingFilter), "categories").GetValue(tf);
                    if (c.NullOrEmpty()) return "";
                    foreach (var x in c)
                    {
                        if (categoriesString != "") categoriesString += "\n";
                        categoriesString += DefDatabase<ThingCategoryDef>.GetNamed(x).LabelCap;
                    }
                }

                return categoriesString;
            }
        }

        public string AllowedDefsString
        {
            get
            {
                if (defsString == null)
                {
                    defsString = "";
                    var tf = parent?.building?.fixedStorageSettings?.filter;
                    if (tf == null) //Log.Warning("LWM.DeepStorage:could not find filter for "+parent.defName);
                        return "";
                    var d = (List<ThingDef>) AccessTools.Field(typeof(ThingFilter), "thingDefs").GetValue(tf);
                    if (d.NullOrEmpty()) return "";
                    foreach (var x in d)
                    {
                        if (defsString != "") defsString += "\n";
                        defsString += x.LabelCap;
                    }
                }

                return defsString;
            }
        }

        public string DisallowedString
        {
            get
            {
                if (disallowedString == null)
                {
                    disallowedString = "";
                    var tf = parent?.building?.fixedStorageSettings?.filter; // look familiar yet?
                    if (tf == null) //Log.Warning("LWM.DeepStorage:could not find filter for "+parent.defName);
                        return "";
                    var c = (List<string>) AccessTools.Field(typeof(ThingFilter), "disallowedCategories").GetValue(tf);
                    if (!c.NullOrEmpty())
                        foreach (var x in c)
                        {
                            if (disallowedString != "") disallowedString += "\n";
                            disallowedString += DefDatabase<ThingCategoryDef>.GetNamed(x).LabelCap;
                        }

                    var d = (List<ThingDef>) AccessTools.Field(typeof(ThingFilter), "disallowedThingDefs").GetValue(tf);
                    if (!d.NullOrEmpty())
                        foreach (var x in d)
                        {
                            if (defsString != "") defsString += "\n";
                            defsString += x.LabelCap;
                        }
                }

                return disallowedString;
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var s in base.SpecialDisplayStats(req)) yield return s;
            if (DeepStorageCategory == null)
            {
                DeepStorageCategory = DefDatabase<StatCategoryDef>.GetNamed("LWM_DS_Stats");
                if (DeepStorageCategory == null)
                {
                    Log.Warning("LWM.DeepStorage: Stat Category FAILED to load.");
                    yield break;
                }
            }

            yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_maxNumStacks".Translate().ToString(),
                size > 1
                    ? "LWM_DS_TotalAndPerCell".Translate(maxNumberStacks * size, maxNumberStacks).ToString()
                    : maxNumberStacks.ToString(),
                "LWM_DS_maxNumStacksDesc".Translate(),
                11 /*display priority*/);
            if (minNumberStacks > 2)
                yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_minNumStacks".Translate(),
                    size > 1
                        ? "LWM_DS_TotalAndPerCell".Translate(minNumberStacks * size, minNumberStacks).ToString()
                        : minNumberStacks.ToString(),
                    "LWM_DS_minNumStacksDesc".Translate(minNumberStacks * size),
                    10 /*display priority*/); //todo: more info here would be good!
            if (maxTotalMass > 0f)
                yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_maxTotalMass".Translate(),
                    size > 1
                        ? "LWM_DS_TotalAndPerCell".Translate(kg(maxTotalMass * size), kg(maxTotalMass)).ToString()
                        : kg(maxTotalMass),
                    "LWM_DS_maxTotalMassDesc".Translate(), 9);
            if (maxMassOfStoredItem > 0f)
                yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_maxMassOfStoredItem".Translate(),
                    kg(maxMassOfStoredItem),
                    "LWM_DS_maxMassOfStoredItemDesc".Translate(), 8);
            if (AllowedCategoriesString != "")
                yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_allowedCategories".Translate(),
                    AllowedCategoriesString,
                    "LWM_DS_allowedCategoriesDesc".Translate(), 7);
            if (AllowedDefsString != "")
                yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_allowedDefs".Translate(),
                    AllowedDefsString,
                    "LWM_DS_allowedDefsDesc".Translate(), 6);
            if (DisallowedString != "")
                yield return new StatDrawEntry(DeepStorageCategory, "LWM_DS_disallowedStuff".Translate(),
                    DisallowedString,
                    "LWM_DS_disallowedStuffDesc".Translate(), 5);
//            if (parent?.building?.fixedStorageSettings?.filter
        }

        private string kg(float s)
        {
            if (altStat == null) return "LWM_DS_kg".Translate(s);
            return "LWM_DS_BulkEtcOf".Translate(s, altStat.label);
        }
        /************************* Done with Stat window ***********************/


        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            parent = parentDef; // no way to actually get this via def :p
            size = parentDef.Size.Area;
        }
    }

    public enum GuiOverlayType : byte
    {
        Normal,
        CountOfAllStacks, // Centered on DSU
        CountOfStacksPerCell, // Standard overlay position for each cell
        SumOfAllItems, // Centered on DSU
        SumOfItemsPerCell, // For e.g., Big Shelf
        None // Some users may want this
    }
}