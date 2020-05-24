using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    [StaticConstructorOnStartup]
    public class DSGUI_ListItem
    {
        private static readonly Texture2D menuIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/Menu");
        private static Texture2D thingIcon;
        private static Color thingColor;
        private static readonly List<FloatMenuOption> orders = new List<FloatMenuOption>();
        private static Thing target;
        private static float height;

        public readonly string label;

        public DSGUI_ListItem(
            Rect inRect,
            Pawn pawn,
            Thing thing,
            Vector3 clickPos,
            List<FloatMenuOption> opts,
            float boxHeight,
            int y)
        {
            height = boxHeight;
            var listRect = new Rect(0.0f, boxHeight * y, inRect.width, boxHeight);
            var graphicRect = new Rect(listRect.LeftPart(0.9f));
            graphicRect.width -= 16;
            var actionRect = new Rect(listRect.RightPart(0.1f));
            actionRect.x -= 16;
            target = thing.GetInnerIfMinified();
            label = thing.Label;

            try
            {
                thingIcon = target.def.uiIcon; 
                thingColor = target.def.uiIconColor;
            }
            catch
            {
                Log.Warning($"[LWM] Thing {thing.def.defName} has no UI icon.");
                thingIcon = Texture2D.blackTexture;
            }

            Widgets.DrawTextureFitted(graphicRect.LeftPart(0.15f).ContractedBy(2f), thingIcon, 1.25f);
            TooltipHandler.TipRegion(graphicRect.RightPart(0.85f), (TipSignal) thing.def.description);
            
            if (DSGUI.Elements.ButtonInvisibleLabeled(Color.white, GameFont.Small, graphicRect.RightPart(0.85f), thing.Label.CapitalizeFirst()))
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(thing);
                Find.WindowStack.TryRemove(typeof(DSGUI_ListModal));
            }
            
            if (Mouse.IsOver(graphicRect))
                Widgets.DrawHighlight(graphicRect);

            if (orders.NullOrEmpty())
                DSGUI_AHLO.AddHumanlikeOrdersForThing(thing, clickPos, pawn, orders);
            
            if (orders.Count > 0) 
            {
                if (DSGUI.Elements.ButtonImageFittedScaled(actionRect, menuIcon, 1.25f))
                {
                    var floatMenuMap = new FloatMenuMap(orders, "Orders", UI.MouseMapPosition()) {givesColonistOrders = true};
                    Find.WindowStack.Add(floatMenuMap);
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(actionRect, menuIcon, 1.25f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(actionRect, "No orders available");
            }
            
            if (Mouse.IsOver(actionRect))
                Widgets.DrawHighlight(actionRect);
            
            if (y != 0)
                DSGUI.Elements.SeparatorHorizontal(0f, boxHeight * y, inRect.width);
            
            Log.Message("[LWM]" + target.Label);
        }

        public void DoDraw(Rect inRect, float y)
        {
            var listRect = new Rect(0.0f, height * y, inRect.width, height);
            var graphicRect = listRect.LeftPart(0.9f);
            graphicRect.width -= 16;
            var actionRect = listRect.RightPart(0.1f);
            actionRect.x -= 16;

            GUI.color = thingColor;
            Widgets.DrawTextureFitted(graphicRect.LeftPart(0.15f).ContractedBy(2f), thingIcon, 1.25f);
            TooltipHandler.TipRegion(graphicRect.RightPart(0.85f), (TipSignal) target.def.description);
            GUI.color = Color.white;
            
            if (DSGUI.Elements.ButtonInvisibleLabeled(Color.white, GameFont.Small, graphicRect.RightPart(0.85f), label.CapitalizeFirst()))
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(target);
                Find.WindowStack.TryRemove(typeof(DSGUI_ListModal));
            }
            
            if (Mouse.IsOver(graphicRect))
                Widgets.DrawHighlight(graphicRect);
            
            if (orders.Count > 0) 
            {
                if (DSGUI.Elements.ButtonImageFittedScaled(actionRect, menuIcon, 1.25f))
                {
                    var floatMenuMap = new FloatMenuMap(orders, "Orders", UI.MouseMapPosition()) {givesColonistOrders = true};
                    Find.WindowStack.Add(floatMenuMap);
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(actionRect, menuIcon, 1.25f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(actionRect, "No orders available");
            }
            
            if (Mouse.IsOver(actionRect))
                Widgets.DrawHighlight(actionRect);
            
            if (y != 0)
                DSGUI.Elements.SeparatorHorizontal(0f, height * y, listRect.width);
        }
    }
}