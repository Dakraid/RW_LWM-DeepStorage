using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

/// <summary>
///   Two Dialog windows to allow whiny RimWorld players to change settings for the Deep Storage units.
///   Basic idea:
///     on load, check if modifying units is turned on.  If so, once defs are loaded, do second
///     pass through the Setting's ExposeData() and this time, parse "DSU_LWM_defName_fieldName",
///     populate a dictionary with the default values, and change the defs on the fly.
///
///   This file contains the two dialog windows, keeps the default settings, and handles making the def changes.
///   The call to re-read the storage settings is done in ModSettings.cs, and the call to ExposeDSUSettings is
///     done in ModSettings' ExposeData.
/// </summary>

namespace LWM.DeepStorage
{
    public class Dialog_DS_Settings : Window {
        public Dialog_DS_Settings() {
			this.forcePause = true;
			this.doCloseX = true;
            this.doCloseButton = true;
			this.closeOnClickedOutside = true;
			this.absorbInputAroundWindow = true;
        }

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(900f, 700f);
			}
		}

        public override void DoWindowContents(Rect inRect)
		{
            var contentRect = new Rect(0, 0, inRect.width, inRect.height - (CloseButSize.y + 10f)).ContractedBy(10f);
//            var scrollViewVisible = new Rect(0f, titleRect.height, contentRect.width, contentRect.height - titleRect.height);
            bool scrollBarVisible = totalContentHeight > contentRect.height;
            var scrollViewTotal = new Rect(0f, 0f, contentRect.width - (scrollBarVisible ? ScrollBarWidthMargin : 0), totalContentHeight);
            Widgets.DrawHighlight(contentRect);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, scrollViewTotal);
            float curY = 0f;
            Rect r=new Rect(0,curY,scrollViewTotal.width, LabelHeight);

//            r=new Rect(0,curY,scrollViewTotal.width, LabelHeight);
            Widgets.CheckboxLabeled(r, "TURN ON USER-MODIFIED BUILDING SETTINGS:", ref Settings.allowPerDSUSettings);//TODO
            TooltipHandler.TipRegion(r, "This lets you edit the properties of each Deep Storage building!  Don't break anything, okay? TURNING THIS OFF WILL RESET ALL SETTINGS TO DEFAULT.");
            curY+=LabelHeight+1f;
            if (!Settings.allowPerDSUSettings) {
                r=new Rect(5f, curY, scrollViewTotal.width-10f, LabelHeight);
                Widgets.Label(r, "Note: No settings will be saved!");
                curY+=LabelHeight;
            }
            Widgets.DrawLineHorizontal(0f, curY, scrollViewTotal.width);
            curY+=10f;

            // todo: make this static?
            //List<ThingDef> l=DefDatabase<ThingDef>.AllDefsListForReading.Where(ThingDef d => d.Has

            // Roll my own buttons, because dammit, I want left-justified buttons:
            GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
            var bg=ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG", true);
            var bgmouseover=ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover", true);
            var bgclick=ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick", true);
            foreach (ThingDef u in Settings.allDeepStorageUnits) {

                r=new Rect(5f, curY, (scrollViewTotal.width)*2/3-7f, LabelHeight);
                // Draw button-ish background:
                Texture2D atlas = bg;
				if (Mouse.IsOver(r))
				{
					atlas = bgmouseover;
					if (Input.GetMouseButton(0))
					{
						atlas = bgclick;
					}
				}
				Widgets.DrawAtlas(r, atlas);
                // button text:
                Widgets.Label(r, u.label+" (defName: "+u.defName+")");
                // button clickiness:
                if (Widgets.ButtonInvisible(r)) {
                    Find.WindowStack.Add(new Dialog_DSU_Settings(u));
                }
                // Reset button:
                r=new Rect((scrollViewTotal.width)*2/3+2f,curY, (scrollViewTotal.width)/3-7f, LabelHeight);
                if (IsDSUChanged(u) && Widgets.ButtonText(r, "ResetBinding".Translate())) {
                    ResetDSUToDefaults(u.defName);
                }
                curY+=LabelHeight+2f;
            }
            GenUI.ResetLabelAlign();
            
            Widgets.EndScrollView();
            r=new Rect(10f, inRect.height-CloseButSize.y-5f, inRect.width/3, CloseButSize.y);
            if (defaultDSUValues.Count>0 && Widgets.ButtonText(r, "LWM.ResetAllToDefault".Translate())) {
                Utils.Mess(Utils.DBF.Settings, "Resetting all per-building storage settings to default:");
                ResetAllToDefaults();
            }
            totalContentHeight = curY;
        }

        private class Dialog_DSU_Settings : Window {
            public Dialog_DSU_Settings(ThingDef def) {
                this.forcePause = true;
                this.doCloseX = true;
                this.doCloseButton = false;
                this.closeOnClickedOutside = true;
                this.absorbInputAroundWindow = true;
                this.def=def;

                SetTempVars();
            }
            private void SetTempVars() {
                tmpLabel=def.label;
                tmpMaxNumStacks=def.GetCompProperties<Properties>().maxNumberStacks;
                tmpMaxTotalMass=def.GetCompProperties<Properties>().maxTotalMass;
                tmpMaxMassStoredItem=def.GetCompProperties<Properties>().maxMassOfStoredItem;
                tmpShowContents=def.GetCompProperties<Properties>().showContents;
                tmpStoragePriority=def.building.defaultStorageSettings.Priority;
                tmpOverlayType=def.GetCompProperties<Properties>().overlayType;
            }

            private void SetTempVarsToDefaults() {
                SetTempVars();
                string k="DSU_"+def.defName;
//                if (defaultDSUValues.ContainsKey(k+"_label")) tmpLabel=(string)defaultDSUValues[k+"label"];
                HelpSetTempVarToDefault<string>(ref tmpLabel, "label");
                HelpSetTempVarToDefault<int>(ref tmpMaxNumStacks, "maxNumStacks");
                HelpSetTempVarToDefault<float>(ref tmpMaxTotalMass, "maxTotalMass");
                HelpSetTempVarToDefault<float>(ref tmpMaxMassStoredItem, "maxMassStoredItem");
                HelpSetTempVarToDefault<bool>(ref tmpShowContents, "showContents");
                HelpSetTempVarToDefault<StoragePriority>(ref tmpStoragePriority, "storagePriority");
                HelpSetTempVarToDefault<LWM.DeepStorage.GuiOverlayType>(ref tmpOverlayType, "overlayType");
                //HelpSetTempVarToDefault<>(ref tmp, "");
            }
            private bool AreTempVarsDefaults() {
                var cp=def.GetCompProperties<LWM.DeepStorage.Properties>();
                if (tmpLabel!=def.label) return false;
                if (tmpMaxMassStoredItem!=cp.maxMassOfStoredItem ||
                    tmpMaxNumStacks!=cp.maxNumberStacks ||
                    tmpMaxTotalMass!=cp.maxTotalMass ||
                    tmpOverlayType!=cp.overlayType ||
                    tmpShowContents!=cp.showContents
                    ) return false;
                if (tmpStoragePriority!=def.building.defaultStorageSettings.Priority) return false;
                return true;
            }
            private void HelpSetTempVarToDefault<T>(ref T v, string keylet) { // MEH.
                string key="DSU_"+def.defName+"_"+keylet;
                if (defaultDSUValues.ContainsKey(key)) {
                    v=(T)defaultDSUValues[key];
                }
            }
            public override Vector2 InitialSize
            {
                get
                {
                    return new Vector2(900f, 700f);
                }
            }

            public override void DoWindowContents(Rect inRect) // For a specific DSU
            {
                var contentRect = new Rect(0, 0, inRect.width, inRect.height - (CloseButSize.y + 10f)).ContractedBy(10f);
                var l = new Listing_Standard();
                l.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height-CloseButSize.y-5f));
                l.Label(def.label);
                l.GapLine();
                // Much TODO, so wow:
                tmpLabel=l.TextEntryLabeled("Label", tmpLabel);
                string tmpstring=null;
                //TODO: redo, include defaults:
                l.TextFieldNumericLabeled("Maximum Number of Stacks per Cell", ref tmpMaxNumStacks, ref tmpstring,0);
                tmpstring=null;
                l.TextFieldNumericLabeled<float>("Maximum Total Mass per Cell", ref tmpMaxTotalMass, ref tmpstring,0f);
                tmpstring=null;
                l.TextFieldNumericLabeled<float>("Maximum Mass of any Stored Item", ref tmpMaxMassStoredItem, ref tmpstring,0f);
                l.CheckboxLabeled("Show contents for this building", ref tmpShowContents);
                l.GapLine();
                l.EnumRadioButton(ref tmpOverlayType, "What kind of count overlay (little white numbers) to use:");
                l.GapLine();
                l.EnumRadioButton(ref tmpStoragePriority, "Storage Priority for this building");
                l.GapLine();
//                l.Label("What Items are Allowed?:");
                l.End();

//                Widgets.Label(contentRect, "Testing "+def.label+"...");
//                Widgets.DrawLineHorizontal(0, 22f, inRect.width-5f);
//                curY=25f;
//                tmpLabel=Widgets.TextArea(Rect rect, string text);
                //...
                
                
/*                var closeRect = new Rect(inRect.width-CloseButSize.x, inRect.height-CloseButSize.y,CloseButSize.x,CloseButSize.y);
                if (shouldIClose) {
                    shouldIClose=false;
                    Close();
                }
                if (Widgets.ButtonText(closeRect, "Close")) { //todo
                    GUI.FocusControl(null); // unfocus, so that a focused text field may commit its value
                    shouldIClose=true;
                }
                */
                // Cancel button
                var closeRect = new Rect(inRect.width-CloseButSize.x, inRect.height-CloseButSize.y,CloseButSize.x,CloseButSize.y);
                if (Widgets.ButtonText(closeRect, "CancelButton".Translate())) {
                    Close();
                }
                // Accept button - with accompanying logic
                closeRect = new Rect(inRect.width-(2*CloseButSize.x+5f), inRect.height-CloseButSize.y,CloseButSize.x,CloseButSize.y);
                if (Widgets.ButtonText(closeRect, "AcceptButton".Translate())) {
                    GUI.FocusControl(null); // unfocus, so that a focused text field may commit its value
                    Utils.Warn(Utils.DBF.Settings, "\"Accept\" button selected: changing values for "+def.defName);
                    TestAndUpdate("label", tmpLabel, ref def.label);
                    TestAndUpdate("maxNumStacks", tmpMaxNumStacks, ref def.GetCompProperties<Properties>().maxNumberStacks);
                    TestAndUpdate("maxTotalMass", tmpMaxTotalMass, ref def.GetCompProperties<Properties>().maxTotalMass);
                    TestAndUpdate("maxMassStoredItem", tmpMaxMassStoredItem, ref def.GetCompProperties<Properties>().maxMassOfStoredItem);
                    TestAndUpdate("showContents", tmpShowContents, ref def.GetCompProperties<Properties>().showContents);
                    TestAndUpdate("overlayType", tmpOverlayType, ref def.GetCompProperties<Properties>().overlayType);
                    StoragePriority tmpSP=def.building.defaultStorageSettings.Priority; // hard to access private field directly
                    TestAndUpdate("storagePriority", tmpStoragePriority, ref tmpSP);
                    def.building.defaultStorageSettings.Priority=tmpSP;
                    Close();
                }
                // Reset to Defaults
                closeRect = new Rect(inRect.width-(4*CloseButSize.x+10f), inRect.height-CloseButSize.y,2*CloseButSize.x,CloseButSize.y);
                if (!AreTempVarsDefaults() && Widgets.ButtonText(closeRect, "ResetBinding".Translate())) {
                    SetTempVarsToDefaults();
                    //ResetDSUToDefaults(def.defName);
                    //SetTempVars();
                }
            }

            private void TestAndUpdate<T>(string keylet, T value, ref T origValue) where T : IComparable {
//                if (value.CompareTo(origValue)==0) return;//TODO
                string key="DSU_"+def.defName+"_"+keylet;
                if (value.CompareTo(origValue)==0) {
                    Utils.Mess(Utils.DBF.Settings,"  No change: "+key);
                    return;
                }
                Utils.Mess(Utils.DBF.Settings,"changing value for "+key+" from "+origValue+" to "+value);
                // "origValue" may be suspect - user could already have changed it once.  So:
                //    (this IS assignment by value, right?)
                T defaultValue=(defaultDSUValues.ContainsKey(key)?(T)defaultDSUValues[key]:origValue);
                origValue=value;
                // if the user reset to originald defaul values, remove the default values key
                if (defaultValue.CompareTo(origValue)==0 && defaultDSUValues.ContainsKey(key)) {
                    defaultDSUValues.Remove(key);
                } else if (!defaultDSUValues.ContainsKey(key)) {
                    defaultDSUValues[key]=defaultValue;
                }
            }

            ThingDef def;
            bool shouldIClose=false;
            string tmpLabel;
            int tmpMaxNumStacks;
            float tmpMaxTotalMass;
            float tmpMaxMassStoredItem;
            bool tmpShowContents;
            LWM.DeepStorage.GuiOverlayType tmpOverlayType;
            StoragePriority tmpStoragePriority;
        }
        private static void ResetDSUToDefaults(string defName) {
            var allKeys=new List<string>(defaultDSUValues.Keys);
            Utils.Warn(Utils.DBF.Settings, "Resetting DSU to default: "+(defName==null?"ALL":defName)
                       +" ("+allKeys.Count+" defaults to search)");
            while (allKeys.Count > 0) {
                var key=allKeys.Last();
                var value = defaultDSUValues[key];
                string s=key.Remove(0,4); // string off first DSU_
                var t=s.Split('_');
                string prop=t[t.Length-1]; // LWM_Big_Shelf_label ->  grab label
                string keyDefName=string.Join("_", t.Take(t.Length-1).ToArray()); // put defName back together
                Utils.Mess(Utils.DBF.Settings, "Checking key "+key+" (defName of "+keyDefName+")");
                if (defName==null || defName=="" || defName==keyDefName) {
                    Log.Message("LWM.DeepStorage: Resetting "+keyDefName+"'s "+prop+" to default: "+value);
                    var def=DefDatabase<ThingDef>.GetNamed(keyDefName);
                    if (prop=="label") {
                        def.label=(string)(value);
                    } else if (prop=="maxNumStacks") {
                        def.GetCompProperties<Properties>().maxNumberStacks=(int)(value);
                    } else if (prop=="maxTotalMass") {
                        def.GetCompProperties<Properties>().maxTotalMass=(float)(value);
                    } else if (prop=="maxMassStoredItem") {
                        def.GetCompProperties<Properties>().maxMassOfStoredItem=(float)(value);
                    } else if (prop=="showContents") {
                        def.GetCompProperties<Properties>().showContents=(bool)(value);
                    } else if (prop=="storagePriority") {
                        def.building.defaultStorageSettings.Priority=(StoragePriority)(value);
                    } else if (prop=="overlayType") {
                        def.GetCompProperties<Properties>().overlayType=(LWM.DeepStorage.GuiOverlayType)(value);
                    } else {
                        Log.Error("LWM.DeepStorage: FAILED TO RESET OPTION TO DEFAULT: "+key);
                    }
                    defaultDSUValues.Remove(key);
                }
                allKeys.RemoveLast();
            }
        }
        private static bool IsDSUChanged(ThingDef d) {
            foreach (string k in defaultDSUValues.Keys) {
                string s=k.Remove(0,4); // strip off DSU_
                var t=s.Split('_');
                string keyDefName=string.Join("_", t.Take(t.Length-1).ToArray());
                if (keyDefName==d.defName) return true;
            }
            return false;
        }
        private static void ResetAllToDefaults() {
            ResetDSUToDefaults(null);
        }

        public static void ExposeDSUSettings(List<ThingDef> units) {
            foreach (ThingDef u in units) {
                string k1=u.defName;
                ExposeDSUSetting<string>(k1+"_label",ref u.label);
                ExposeDSUSetting(k1+"_maxNumStacks", ref u.GetCompProperties<Properties>().maxNumberStacks);
                ExposeDSUSetting(k1+"_maxTotalMass", ref u.GetCompProperties<Properties>().maxTotalMass);
                ExposeDSUSetting(k1+"_maxMassStoredItem", ref u.GetCompProperties<Properties>().maxMassOfStoredItem);
                ExposeDSUSetting(k1+"_showContents", ref u.GetCompProperties<Properties>().showContents);
                ExposeDSUSetting(k1+"_overlayType", ref u.GetCompProperties<Properties>().overlayType);
                StoragePriority tmpSP=u.building.defaultStorageSettings.Priority; // hard to access private field directly
                ExposeDSUSetting<StoragePriority>(k1+"_storagePriority", ref tmpSP);
                u.building.defaultStorageSettings.Priority=tmpSP;
                
/*                    string key="DSU_"+u.defName+"_label";
                      string value=u.label;
                      string defaultValue=(defaultDSUValues.ContainsKey(key)?(string)defaultDSUValues[key]:value);
                      Scribe_Values.Look(ref value, key, defaultValue);
                      if (defaultValue != value && !defaultDSUValues.ContainsKey(key)) {
                      defaultDSUValues[key]=defaultValue;
                      }
*/                   
            }
            
            
        }
        // Only ONE DSU Setting:
        private static void ExposeDSUSetting<T>(string keylet, ref T value, object origValue=null) where T : IComparable {
            string key = "DSU_"+keylet;
            T defaultValue=(defaultDSUValues.ContainsKey(key)?(T)defaultDSUValues[key]:value);
            Scribe_Values.Look(ref value, key, defaultValue);
            if (defaultValue.CompareTo(value) != 0 && !defaultDSUValues.ContainsKey(key)) {
//                Log.Message("-->"+key+" storing default value of "+defaultValue);//TODO
//                Log.Warning("        Current value: "+value);
                defaultDSUValues[key]=defaultValue;
            }
        }

        
        private float totalContentHeight=1000f;
        private static Vector2 scrollPosition;
        
		private const float TopAreaHeight = 40f;
		private const float TopButtonHeight = 35f;
		private const float TopButtonWidth = 150f;
        private const float ScrollBarWidthMargin = 18f;
        private const float LabelHeight=22f;

        // Actual Logic objects:
        //   Default values for DSU objects, so that when saving mod settings, we know what the defaults are.
        //   Only filled in as user changes the values.
        public static Dictionary<string, object> defaultDSUValues=new Dictionary<string, object>();
    }

        
}