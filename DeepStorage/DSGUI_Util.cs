using System.Collections.Generic;
using System.Reflection;
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
            KeyValuePair<Thing, IntVec3> thingEntry,
            List<FloatMenuOption> opts,
            int boxHeight,
            int y)
        {
            var entryRect = new Rect(0.0f, boxHeight * y, inRect.width - 16, boxHeight);

            if (Mouse.IsOver(entryRect))
                Widgets.DrawHighlight(entryRect);

            TooltipHandler.TipRegion(entryRect, (TipSignal) thingEntry.Key.def.description);

            Texture2D thingIcon;
            try
            {
                thingIcon = thingEntry.Key.def.uiIcon;
                GUI.color = thingEntry.Key.def.uiIconColor;
            }
            catch
            {
                Log.Message($"[LWM] Thing {thingEntry.Key.def.defName} has no UI icon.");
                thingIcon = Texture2D.blackTexture;
            }

            // We keep the LeftPart for the future, but right now it doesn't do anything
            var graphicRect = entryRect.LeftPart(0.9f);
            Widgets.DrawTextureFitted(graphicRect.LeftPart(0.2f), thingIcon, 1f);
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(graphicRect.RightPart(0.8f).ContractedBy(4f), thingEntry.Key.Label.CapitalizeFirst());
            
            var actionRect = entryRect.RightPart(0.1f);
            
            var menuIcon = ContentFinder<Texture2D>.Get("UI/Buttons/MainButtons/Menu", true);
            if (Widgets.ButtonImageFitted(actionRect, menuIcon))
            {
                Log.Message("[LWM] Clickity Click!");
                var origParams = new object[] {thingEntry.Value, pawn, opts};
                Patch_FloatMenuMakerMap.SetPosition(thingEntry.Key, thingEntry.Value);
                AHlO.Invoke(null, origParams);
                Patch_FloatMenuMakerMap.SetPosition(thingEntry.Key, IntVec3.Invalid);
            }

            /*
            if (Widgets.ButtonInvisible(entryRect)) {}
            */

            if (y == 0) return;

            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(0.0f, boxHeight * y, inRect.width);
            GUI.color = Color.white;
        }
    }
}