using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI_ListModal : Window
    {
        private static readonly Vector2 defaultScreenSize = new Vector2(1920, 1080);
        private static readonly Vector2 modalSize = new Vector2(360, 480);
        public override Vector2 InitialSize => new Vector2(modalSize.x * (Screen.width / defaultScreenSize.x), modalSize.y * (Screen.height / defaultScreenSize.y));

        private const float boxHeight = 48f;
        private const float searchClearPadding = 16f;

        private static Vector2 scrollPosition;
        private static float RecipesScrollHeight;
        private static string searchString = "";
        public Rect GizmoListRect;
        
        private static Vector3 cpos;
        private static Vector3 lpos;
        private static Pawn pawn;
        private static List<Thing> thingList;
        private static List<FloatMenuOption> opts;
        private readonly List<DSGUI_ListItem> rows = new List<DSGUI_ListItem>();

        public DSGUI_ListModal(Pawn p, List<Thing> lt, Vector3 pos, List<FloatMenuOption> op)
        {
            closeOnClickedOutside = true;
            doCloseX = true;
            resizeable = true;
            draggable = true;
            
            if (p == null)
                return;

            cpos = pos;
            pawn = p;
            opts = op;
            
            lt.RemoveAll(t => t.def.category != ThingCategory.Item || t is Mote);
            thingList = new List<Thing>(lt);
            
            rows.Clear();
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            var y = 0;
            var innerRect = inRect;
            innerRect.y += 8f;
            innerRect.height -= 16f;
            
            GizmoListRect = innerRect.AtZero();
            GizmoListRect.y += scrollPosition.y;

            // Scrollable List
            var scrollRect = new Rect(innerRect);
            scrollRect.y += 3f;
            scrollRect.x += 8f;
            scrollRect.height -= 49f;
            scrollRect.width -= 16f;
            
            var listRect = new Rect(0.0f, 0.0f, scrollRect.width, RecipesScrollHeight);

            if (rows.NullOrEmpty() && !lpos.Equals(cpos)) 
            {
                lpos = cpos;
            
                foreach (var thing in thingList)
                {
                    try
                    {
                        rows.Add(new DSGUI_ListItem(pawn, thing, cpos, boxHeight));
                    }
                    catch (Exception ex)
                    {
                        var rect5 = scrollRect.ContractedBy(-4f);
                        Widgets.Label(rect5, "Oops, something went wrong!");
                        Log.Warning(ex.ToString());
                    }

                    ++y;
                }
            }
            
            Widgets.DrawBox(scrollRect);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, listRect);
            GUI.BeginGroup(listRect);

#if DEBUG
            var drawCount = 0;
            Log.Message("[LWM] Total count of items: " + rows.Count);
#endif
            if (searchString.NullOrEmpty())
                for (var i = 0; i < rows.Count; i++)
                {
                    if (!rows[i].GetRect(listRect, i).Overlaps(GizmoListRect)) continue;
                    
                    rows[i].DoDraw(listRect, i);
#if DEBUG
                    ++drawCount;
#endif
                }
            else
                for (var i = 0; i < rows.Count; i++)
                {
                    if (!rows[i].GetRect(listRect, i).Overlaps(GizmoListRect)) continue;
                    if (!(rows[i].label.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                        
                    rows[i].DoDraw(listRect, i);
                }
#if DEBUG
            Log.Message("[LWM] Drawn count of items: " + drawCount);
#endif
            RecipesScrollHeight = boxHeight * rows.Count;
            
            GUI.EndGroup();
            Widgets.EndScrollView();
            
            // Search
            var searchRect = new Rect(innerRect);
            searchRect.y += scrollRect.height + 16f;
            searchRect.x += 8f;
            searchRect.height = 28f;
            searchRect.width -= 40f + searchClearPadding; // 16f for padding of 8f on each side + 28f for the clear button
            
            DSGUI.Elements.InputField("Search", searchRect, ref searchString);

            searchRect.x = searchRect.width + 6f + searchClearPadding;
            searchRect.width = 28f;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (Widgets.ButtonImageFitted(searchRect, Widgets.CheckboxOffTex))
                searchString = "";
            
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}