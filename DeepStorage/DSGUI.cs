using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DSGUI
    {
        public class ContextMenuStorage
        {
            public static bool Create(Vector3 clickPosition, Pawn pawn, List<FloatMenuOption> opts)
            {
                var c = IntVec3.FromVector3(clickPosition);

                if ((c.GetSlotGroup(pawn.Map)?.parent as ThingWithComps)?.AllComps
                    .FirstOrDefault(x => x is IHoldMultipleThings.IHoldMultipleThings) == null)
                    return false;

                var thingList = c.GetThingList(pawn.Map);

                if (!opts.NullOrEmpty())
                    opts.Clear();
                
                Find.WindowStack.Add(new DSGUI_ListModal(pawn, thingList, clickPosition, opts));
                return true;
            }
        }
    }
}