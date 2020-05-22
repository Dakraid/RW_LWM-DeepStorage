using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI_SubMenuModal : Window
    {
        private const int boxHeight = 32;

        private static readonly Vector2 defaultScreenSize = new Vector2(1920, 1080);
        private static readonly Vector2 modalSize = new Vector2(400, 200);

        private static Vector2 scrollPosition;
        private static float RecipesScrollHeight;
        
        private static Vector3 cpos;
        private static Pawn pawn;
        private static List<FloatMenuOption> opts;

        public DSGUI_SubMenuModal(Pawn p, Vector3 pos, List<FloatMenuOption> op)
        {
            closeOnClickedOutside = true;
            doCloseButton = true;
            resizeable = true;
            draggable = true;

            if (p == null)
                return;

            cpos = pos;
            pawn = p;
            opts = op;
        }

        public override Vector2 InitialSize => new Vector2(modalSize.x * (Screen.width / defaultScreenSize.x), modalSize.y * (Screen.height / defaultScreenSize.y));

        public override void DoWindowContents(Rect inRect)
        {
            // Search
            /*
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
            */
            
            var scrollRect = inRect.ContractedBy(4f);
            var listRect = new Rect(0.0f, 0.0f, scrollRect.width, RecipesScrollHeight);
            Widgets.DrawBox(scrollRect);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, listRect);
            GUI.BeginGroup(listRect);
            var y = 0;
                
            foreach (var opt in opts)
            {
                try
                { 
                    DSGUI_Util.GenerateOptsListing(listRect, pawn, cpos, opt, boxHeight, y);
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