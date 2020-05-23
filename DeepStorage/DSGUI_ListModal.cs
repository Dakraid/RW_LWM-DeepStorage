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

        private static readonly Vector2 defaultScreenSize = new Vector2(1920, 1080);
        private static readonly Vector2 modalSize = new Vector2(360, 480);

        private static Vector2 scrollPosition;
        private static float RecipesScrollHeight;
        private static string searchString;
        
        private static Vector3 cpos;
        private static Pawn pawn;
        private static List<Thing> thingList;
        private static List<FloatMenuOption> opts;

        public DSGUI_ListModal(Pawn p, List<Thing> lt, Vector3 pos, List<FloatMenuOption> op)
        {
            closeOnClickedOutside = true;
            doCloseButton = true;
            //doCloseX = true;
            resizeable = true;
            draggable = true;

            // optionalTitle = lt.Find(t => t is Building).Label;
            
            if (p == null)
                return;

            cpos = pos;
            pawn = p;
            opts = op;
            
            lt.RemoveAll(t => t.def.category != ThingCategory.Item);
            thingList = lt;
        }

        public override Vector2 InitialSize => new Vector2(modalSize.x * (Screen.width / defaultScreenSize.x), modalSize.y * (Screen.height / defaultScreenSize.y));
        
        public override void DoWindowContents(Rect inRect)
        {
            // UpdateMouse();
            var innerRect = inRect;
            innerRect.height -= 48f;
            
            // Search
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
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, listRect);
            GUI.BeginGroup(listRect);
            var y = 0;
            
            foreach (var thing in thingList.Where(thing => searchString.NullOrEmpty() || thing.Label.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                try
                { 
                    DSGUI_Util.GenerateListing(listRect, pawn, thing, cpos, opts, boxHeight, y);
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
    }
}