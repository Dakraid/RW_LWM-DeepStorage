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
        // Allow calling AddHumanlikeOrders
        private static readonly MethodInfo AHlO = typeof(FloatMenuMakerMap).GetMethod("AddHumanlikeOrders",
            BindingFlags.Static | BindingFlags.NonPublic);
        
        // Allow directly setting Position of things.  And setting it back.
        private static readonly FieldInfo fieldPosition = typeof(Thing).GetField("positionInt",
            BindingFlags.Instance |
            BindingFlags.GetField |
            BindingFlags.SetField |
            BindingFlags.NonPublic);
        
        private static void SetPosition(Thing t, IntVec3 p)
        {
            fieldPosition.SetValue(t, p);
        }
        
        private readonly Texture2D menuIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/Menu");
        private readonly Texture2D thingIcon;
        private readonly Color thingColor = Color.white;
        private readonly List<FloatMenuOption> orders = new List<FloatMenuOption>();
        private readonly Thing target;
        private readonly float height;
        private readonly Pawn pawn;

        public readonly string label;

        public DSGUI_ListItem(
            Pawn p,
            Thing t,
            Vector3 clickPos,
            float boxHeight)
        {
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

            if (!orders.NullOrEmpty()) return;

            GlobalFlag.currThing = t;
            var origParams = new object[] {clickPos, pawn, orders};
            AHlO.Invoke(null, origParams);
            GlobalFlag.currThing = null;
        }

        public void DoDraw(Rect inRect, float y)
        {
            var listRect = new Rect(0.0f, height * y, inRect.width, height);
            var graphicRect = listRect.LeftPart(0.9f);
            graphicRect.width -= 16;
            var actionRect = listRect.RightPart(0.1f);
            actionRect.x -= 16;

            GUI.color = thingColor;
            Widgets.DrawTextureFitted(graphicRect.LeftPart(0.15f).ContractedBy(2f), thingIcon, 1f);
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
                if (DSGUI.Elements.ButtonImageFittedScaled(actionRect, menuIcon, 1f)) DSGUI.Elements.TryMakeFloatMenu(pawn, orders);
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(actionRect, menuIcon, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(actionRect, "No orders available");
            }
            
            if (Mouse.IsOver(actionRect))
                Widgets.DrawHighlight(actionRect);
            
            if (y != 0)
                DSGUI.Elements.SeparatorHorizontal(0f, height * y, listRect.width);
        }

        public Rect GetRect(Rect inRect, float y)
        {
            return new Rect(0.0f, height * y, inRect.width, height);
        }
    }
}