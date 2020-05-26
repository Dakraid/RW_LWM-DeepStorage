using System.Collections.Generic;
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

                if (icon != null)
                {
                    var outerRect = rect;
                    outerRect.width = outerRect.height;
                    Widgets.DrawTextureFitted(outerRect, icon, 1f);
                    rect.width -= outerRect.width;
                    rect.x += outerRect.width;
                }

                if (ShowName)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect.LeftPart(0.2f), name);
                    Text.Anchor = TextAnchor.UpperLeft;
                    rect = rect.RightPart(0.8f);
                }

                GUI.SetNextControlName(name);
                buff = GUI.TextField(rect, buff, max, Text.CurTextAreaStyle);
                var flag = GUI.GetNameOfFocusedControl() == name;
                if (!flag & forceFocus)
                    GUI.FocusControl(name);
                if (((!Input.GetMouseButtonDown(0) ? 0 : !Mouse.IsOver(rect) ? 1 : 0) & (flag ? 1 : 0)) != 0)
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

            public static void SolidColorBG(Rect inRect, Color inColor)
            {
                GUI.DrawTexture(inRect, SolidColorMaterials.NewSolidColorTexture(inColor));
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

            public static void TryMakeFloatMenu(Pawn pawn, List<FloatMenuOption> options)
            {
                if (!pawn.IsColonistPlayerControlled)
                    return;

                if (pawn.Downed)
                {
                    Messages.Message("IsIncapped".Translate((NamedArgument) pawn.LabelCap, (NamedArgument) pawn), pawn, MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    if (pawn.Map != Find.CurrentMap || options.Count == 0)
                        return;

                    var flag = true;

                    var floatMenuOption = (FloatMenuOption) null;
                    foreach (var option in options)
                    {
                        if (option.Disabled || !option.autoTakeable)
                        {
                            flag = false;
                            break;
                        }

                        if (floatMenuOption == null || option.autoTakeablePriority > floatMenuOption.autoTakeablePriority)
                            floatMenuOption = option;
                    }

                    if (flag && floatMenuOption != null)
                    {
                        floatMenuOption.Chosen(true, null);
                    }
                    else
                    {
                        var floatMenuMap = new FloatMenu(options, pawn.LabelCap) {givesColonistOrders = true};
                        Find.WindowStack.Add(floatMenuMap);
                    }
                }
            }
        }
    }
}