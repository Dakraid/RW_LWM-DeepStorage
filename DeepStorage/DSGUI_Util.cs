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
        private static readonly MethodInfo AHlO = typeof(FloatMenuMakerMap).GetMethod("AddHumanlikeOrders",
            BindingFlags.Static | BindingFlags.NonPublic);

        // Credits to Dubwise for this awesome function
        public static bool InputField(
            string name,
            Rect rect,
            ref string buff,
            Texture icon = null,
            int max = 999,
            bool readOnly = false,
            bool forceFocus = false,
            bool ShowName = false)
        {
            if (buff == null)
                buff = "";
            var rect1 = rect;
            if (icon != null)
            {
                var outerRect = rect;
                outerRect.width = outerRect.height;
                Widgets.DrawTextureFitted(outerRect, icon, 1f);
                rect1.width -= outerRect.width;
                rect1.x += outerRect.width;
            }

            if (ShowName)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect.LeftPart(0.2f), name);
                Text.Anchor = TextAnchor.UpperLeft;
                rect1 = rect.RightPart(0.8f);
            }

            GUI.SetNextControlName(name);
            buff = GUI.TextField(rect1, buff, max, Text.CurTextAreaStyle);
            var flag = GUI.GetNameOfFocusedControl() == name;
            if (!flag & forceFocus)
                GUI.FocusControl(name);
            if (((!Input.GetMouseButtonDown(0) ? 0 : !Mouse.IsOver(rect1) ? 1 : 0) & (flag ? 1 : 0)) != 0)
                GUI.FocusControl(null);
            return flag;
        }

        public static void GenerateListing(
            Rect inRect,
            Pawn pawn,
            Thing thing,
            Vector3 clickPos,
            List<FloatMenuOption> opts,
            int boxHeight,
            int y)
        {
            var entryRect = new Rect(0.0f, boxHeight * y, inRect.width, boxHeight);

            if (Mouse.IsOver(entryRect))
                Widgets.DrawHighlight(entryRect);

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
            Widgets.DrawTextureFitted(graphicRect.LeftPart(0.2f), thingIcon, 1f);
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(graphicRect.RightPart(0.8f).ContractedBy(4f), thing.Label.CapitalizeFirst());
            TooltipHandler.TipRegion(graphicRect.RightPart(0.8f), (TipSignal) thing.def.description);
            
            var actionRect = entryRect.RightPart(0.1f);
            actionRect.x -= 16;
            
            if (!opts.NullOrEmpty())
                opts.Clear();
            DSGUI_AHLO.AddHumanlikeOrdersForThing(thing, clickPos, pawn, opts);
            
            if (opts.Count > 0) 
            {
                var menuIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/Menu");
                if (Widgets.ButtonImageFitted(actionRect, menuIcon))
                {
                    var floatMenuMap = new FloatMenuMap(opts, pawn.LabelCap, UI.MouseMapPosition()) {givesColonistOrders = true};
                    Find.WindowStack.Add(floatMenuMap);
                }
            }

            if (y == 0) return;

            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(0.0f, boxHeight * y, inRect.width);
            GUI.color = Color.white;
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