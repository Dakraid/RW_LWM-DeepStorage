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
        public class ContextMenuStorage
        {
            public static bool Create(Vector3 clickPosition, Pawn pawn, List<FloatMenuOption> opts)
            {
                if (Find.WindowStack.IsOpen(typeof(DSGUI_ListModal)))
                    return true;
                
                var c = IntVec3.FromVector3(clickPosition);

                var target = (c.GetSlotGroup(pawn.Map)?.parent as ThingWithComps)?.AllComps.FirstOrDefault(x => x is IHoldMultipleThings.IHoldMultipleThings);
                if (target == null)
                    return false;

                var thingList = new List<Thing>();
                var cells = target.parent.GetSlotGroup().CellsList;

                foreach (var cell in cells) thingList.AddRange(cell.GetThingList(pawn.Map));

                if (thingList.NullOrEmpty())
                    return true;
                
                Find.WindowStack.Add(new DSGUI_ListModal(pawn, thingList, clickPosition, opts));
                return true;
            }
        }
    }
}