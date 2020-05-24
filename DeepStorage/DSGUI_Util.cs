using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI_Util
    {
        private static readonly Texture2D menuIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/Menu");

        public static void GenerateListing(
            Rect inRect,
            Pawn pawn,
            Thing thing,
            Vector3 clickPos,
            List<FloatMenuOption> opts,
            float boxHeight,
            int y)
        {
            var entryRect = new Rect(0.0f, boxHeight * y, inRect.width, boxHeight);

            Texture2D thingIcon;
            try
            {
                var target = thing.GetInnerIfMinified();
                thingIcon = target.def.uiIcon; 
                GUI.color = target.def.uiIconColor;
            }
            catch
            {
                Log.Message($"[LWM] Thing {thing.def.defName} has no UI icon.");
                thingIcon = Texture2D.blackTexture;
            }

            // We keep the LeftPart for the future, but right now it doesn't do anything
            var graphicRect = entryRect.LeftPart(0.9f);
            graphicRect.width -= 16;
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
            
            DSGUI.Elements.SeparatorVertical(0.0f, graphicRect.width, inRect.height);
            
            var actionRect = entryRect.RightPart(0.1f);
            actionRect.x -= 16;
            
            if (!opts.NullOrEmpty())
                opts.Clear();
            
            DSGUI_AHLO.AddHumanlikeOrdersForThing(thing, clickPos, pawn, opts);
            
            if (opts.Count > 0) 
            {
                if (DSGUI.Elements.ButtonImageFittedScaled(actionRect, menuIcon, 1.25f))
                {
                    var floatMenuMap = new FloatMenuMap(opts, "Orders", UI.MouseMapPosition()) {givesColonistOrders = true};
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
            
            if (y == 0) return;
            
            DSGUI.Elements.SeparatorHorizontal(0f, boxHeight * y, inRect.width);
        }

        public static void GenerateOptsListing(
            Rect inRect,
            Pawn pawn,
            Vector3 clickPos,
            FloatMenuOption opt,
            int boxHeight,
            int y)
        {
            var entryRect = new Rect(0.0f, boxHeight * y, inRect.width, boxHeight);

            if (Mouse.IsOver(entryRect))
                Widgets.DrawHighlight(entryRect);
            
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;

            if (Widgets.ButtonText(entryRect, opt.Label))
            {
                //
            }

            if (y == 0) return;

            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(0.0f, boxHeight * y, inRect.width);
            GUI.color = Color.white;
        }
    }
}