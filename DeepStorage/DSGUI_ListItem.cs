﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.EventSystems;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI_ListItem
    {
        // private readonly List<FloatMenuOption> orders = new List<FloatMenuOption>();
        public readonly string label;
        
        // Allow calling AddHumanlikeOrders
        private readonly MethodInfo AHlO = AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders");
        
        private readonly float height;
        private readonly float iconScale;

        private readonly Texture2D menuIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/Menu");
        private readonly Pawn pawn;
        private readonly Thing target;
        private readonly Color thingColor = Color.white;
        private readonly Texture2D thingIcon;

        public object this[int i] => null;

        public DSGUI_ListItem(
            Pawn p,
            Thing t,
            Vector3 clickPos,
            List<FloatMenuOption> options,
            float boxHeight)
        {
            DSGUI.GlobalStorage.ctorRunning = true;
            iconScale = Settings.newNRC_IconScaling;
            height = boxHeight;
            target = t.GetInnerIfMinified();
            label = t.Label;
            pawn = p;

            try
            {
                thingIcon = target.def.uiIcon;
                thingColor = target.def.uiIconColor;
            }
            catch
            {
                Log.Warning($"[LWM] Thing {t.def.defName} has no UI icon.");
                thingIcon = Texture2D.blackTexture;
            }
            
            AHlO.Invoke(null, new object[] {clickPos, pawn, options});
            DSGUI.GlobalStorage.ctorRunning = false;
        }

        public void DoDraw(Rect inRect, float y, List<FloatMenuOption> options, bool altBG = false)
        {
            var listRect = new Rect(0.0f, height * y, inRect.width, height);

            //if (altBG)
            //    DSGUI.Elements.SolidColorBG(listRect, new Color(1f, 1f, 1f, 0.075f));
            
            var graphicRect = listRect.LeftPart(0.9f);
            graphicRect.width -= 16;
            var actionRect = listRect.RightPart(0.1f);
            actionRect.x -= 16;

            GUI.color = thingColor;
            Widgets.DrawTextureFitted(graphicRect.LeftPart(0.15f).ContractedBy(2f), thingIcon, iconScale);
            TooltipHandler.TipRegion(graphicRect.RightPart(0.85f), (TipSignal) target.def.description);
            GUI.color = Color.white;
            
            if (DSGUI.Elements.ButtonInvisibleLabeled(Color.white, GameFont.Small, graphicRect.RightPart(0.85f), label.CapitalizeFirst()))
            {
                if (pawn.Map != target.Map)
                    return;
                
                Find.Selector.ClearSelection();
                Find.Selector.Select(target);
                Find.WindowStack.TryRemove(typeof(DSGUI_ListModal));
            }

            if (Mouse.IsOver(graphicRect))
                Widgets.DrawHighlight(graphicRect);

            if (options.Count > 0)
            {
                if (DSGUI.Elements.ButtonImageFittedScaled(actionRect, menuIcon, iconScale))
                {
                    DSGUI.Elements.TryMakeFloatMenu(pawn, options, target.LabelCapNoCount);
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(actionRect, menuIcon, iconScale);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(actionRect, "No orders available");
            }

            if (Mouse.IsOver(actionRect))
                Widgets.DrawHighlight(actionRect);

            DSGUI.Elements.SeparatorVertical(graphicRect.xMax, height * y, height);
            
            if (y != 0)
                DSGUI.Elements.SeparatorHorizontal(0f, height * y, listRect.width);
        }

        public Rect GetRect(Rect inRect, float y)
        {
            return new Rect(0.0f, height * y, inRect.width, height);
        }
    }
}