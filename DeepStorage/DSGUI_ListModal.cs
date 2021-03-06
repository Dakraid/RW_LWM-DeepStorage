﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI_ListModal : Window
    {
        private static float boxHeight = 48f;
        private const float searchClearPadding = 16f;
        private static readonly Vector2 defaultScreenSize = new Vector2(1920, 1080);
        private static readonly Vector2 modalSize = new Vector2(360, 480);

        private static Vector2 scrollPosition;
        private static float RecipesScrollHeight;
        private static string searchString = "";

        private readonly Vector3 cpos;
        private static Pawn pawn;
        private static List<Thing> thingList;
        private readonly DSGUI_ListItem[] rows;
        private Rect GizmoListRect;

        public DSGUI_ListModal(Pawn p, List<Thing> lt, Vector3 pos)
        {
            closeOnClickedOutside = true;
            doCloseX = true;
            resizeable = true;
            draggable = true;

            if (p == null)
                return;

            cpos = pos;
            pawn = p;

            lt.RemoveAll(t => t.def.category != ThingCategory.Item || t is Mote);
            thingList = new List<Thing>(lt);

            rows = new DSGUI_ListItem[thingList.Count];
            
            boxHeight = Settings.newNRC_BoxHeight;
        }

        public override Vector2 InitialSize => new Vector2(modalSize.x * (Screen.width / defaultScreenSize.x), modalSize.y * (Screen.height / defaultScreenSize.y));

        public override void DoWindowContents(Rect inRect)
        {
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

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, listRect);
            GUI.BeginGroup(listRect);

            var j = 0;
            for (var i = 0; i < thingList.Count; i++)
            {
                ++j;
                var viewElement = new Rect(0.0f, boxHeight * i, inRect.width, boxHeight);
                if (!viewElement.Overlaps(GizmoListRect)) continue;
                
                if (rows[i] == null)
                    try
                    {
                        rows[i] = new DSGUI_ListItem(pawn, thingList[i], cpos, boxHeight);
                    }
                    catch (Exception ex)
                    {
                        var rect5 = scrollRect.ContractedBy(-4f);
                        Widgets.Label(rect5, "Oops, something went wrong!");
                        Log.Warning(ex.ToString());
                    }

            
                if (searchString.NullOrEmpty()) 
                {
                    rows[i].DoDraw(listRect, i);
                }
                else
                {
                    if (!(rows[i].label.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)) continue;

                    rows[i].DoDraw(listRect, i);
                }
            }

            
            RecipesScrollHeight = boxHeight * thingList.Count;

            GUI.EndGroup();
            Widgets.EndScrollView();
            Widgets.DrawBox(scrollRect);

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