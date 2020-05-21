using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI_ListModal : Window
    {
        private const int boxHeight = 32;
        
        private static readonly Dictionary<Thing, IntVec3> cachedThingEntry = new Dictionary<Thing, IntVec3>();
        private static readonly Vector2 defaultScreenSize = new Vector2(1920, 1080);
        private static readonly Vector2 modalSize = new Vector2(400, 480);
        
        private static Vector2 scrollPositionRecipes;
        private static float RecipesScrollHeight;
        private static string searchString;
        private static Vector2 cachedClickPos;

        private static List<Thing> ThingList;
        private static Pawn pawn;
        private static Vector3 clickPos;
        private static List<FloatMenuOption> opts;
        
        public DSGUI_ListModal(Pawn p, List<Thing> inThingList, Vector3 pos, List<FloatMenuOption> op)
        {
            closeOnClickedOutside = true;
            doCloseButton = true;
            resizeable = true;
            draggable = true;

            if (p == null || inThingList.NullOrEmpty())
                return;
            
            ThingList = inThingList;
            pawn = p;
            clickPos = pos;
            opts = op;
        }

        public override Vector2 InitialSize => new Vector2(modalSize.x * (Screen.width / defaultScreenSize.x), modalSize.y * (Screen.height / defaultScreenSize.y));

        public override void DoWindowContents(Rect inRect)
        {
            // UpdateMouse();
            var innerRect = inRect;
            innerRect.height -= 48f;
            
            var searchRect = innerRect;
            GUI.BeginGroup(searchRect);
            searchRect.height = 28f;
            searchRect.width -= 30f;

            DSGUI_Util.InputField("Search", searchRect, ref searchString);
            Text.Anchor = TextAnchor.MiddleLeft;
            searchRect.x = searchRect.xMax;
            searchRect.width = 30f;
            if (Widgets.ButtonImageFitted(searchRect, Widgets.CheckboxOffTex))
                searchString = "";
            GUI.EndGroup();


            var scrollRect = innerRect;
            scrollRect.height -= 56f;
            scrollRect.y += 48f;
            var listRect = new Rect(0.0f, 0.0f, scrollRect.width, RecipesScrollHeight);
            
            Widgets.DrawBox(scrollRect);
            Widgets.BeginScrollView(scrollRect, ref scrollPositionRecipes, listRect);

            GUI.BeginGroup(listRect);
            var y = 0;
            if (!cachedClickPos.Equals(clickPos))
            {
                cachedClickPos = clickPos;
                cachedThingEntry.Clear();
                foreach (var thing in ThingList)
                    try
                    {
                        cachedThingEntry.Add(thing, IntVec3.FromVector3(clickPos));
                    }
                    catch (Exception ex)
                    {
                        var rect5 = scrollRect.ContractedBy(-4f);
                        Widgets.Label(rect5, "Oops, something went wrong!");
                        Log.Warning(ex.ToString());
                    }
            }

            foreach (var cachedEntry in cachedThingEntry)
            {
                try
                {
                    DSGUI_Util.GenerateListing(listRect, pawn, cachedEntry, opts, boxHeight, y);
                }
                catch (Exception ex)
                {
                    var rect5 = scrollRect.ContractedBy(-4f);
                    Widgets.Label(rect5, "Oops, something went wrong!");
                    Log.Warning(ex.ToString());
                }

                ++y;
            }
            
            RecipesScrollHeight = boxHeight * y;
            GUI.EndGroup();
            Widgets.EndScrollView();

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /*
        // Unused
        private void UpdateMouse()
        {
            var r = new Rect(0.0f, 0.0f, windowRect.width, windowRect.height).ContractedBy(-5f);
            if (r.Contains(Event.current.mousePosition))
                return;

            var num = GenUI.DistFromRect(r, Event.current.mousePosition);
            if (num <= 95.0)
                return;
                
            Close(false);
        }
        */
    }
}