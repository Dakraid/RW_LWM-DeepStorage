using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public partial class DSGUI
    {
        public class Elements
        {
            // Credits to Dubwise for this awesome function
            public static void InputField(
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
            }

            public static bool ButtonInvisibleLabeled(Color textColor, GameFont textSize, Rect inRect, string label)
            {
                GUI.color = textColor;
                Text.Font = textSize;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(inRect, label);
                Text.Anchor = TextAnchor.UpperLeft;
                return Widgets.ButtonInvisible(inRect.ContractedBy(2f));
            }

            public static void SeparatorHorizontal(float x, float y, float len)
            {
                GUI.color = Color.grey;
                Widgets.DrawLineHorizontal(x, y, len);
                GUI.color = Color.white;
            }

            public static void SeparatorVertical(float x, float y, float len)
            {
                GUI.color = Color.grey;
                Widgets.DrawLineVertical(x, y, len);
                GUI.color = Color.white;
            }
            
            public static bool ButtonImageFittedScaled(Rect butRect, Texture2D tex, float scale)
            {
                return ButtonImageFittedScaled(butRect, tex, Color.white, scale);
            }

            public static bool ButtonImageFittedScaled(Rect butRect, Texture2D tex, Color baseColor, float scale)
            {
                return ButtonImageFittedScaled(butRect, tex, baseColor, GenUI.MouseoverColor, scale);
            }

            public static bool ButtonImageFittedScaled(
                Rect butRect,
                Texture2D tex,
                Color baseColor,
                Color mouseoverColor,
                float scale)
            {
                GUI.color = !Mouse.IsOver(butRect) ? baseColor : mouseoverColor;
                Widgets.DrawTextureFitted(butRect, tex, scale);
                GUI.color = baseColor;
                return Widgets.ButtonInvisible(butRect);
            }
        }
    }
}