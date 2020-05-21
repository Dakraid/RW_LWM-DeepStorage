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
            public static bool Create(Vector3 clickPosition, IntVec3 c, Pawn pawn, List<FloatMenuOption> opts)
            {
                c = IntVec3.FromVector3(clickPosition);

                if ((c.GetSlotGroup(pawn.Map)?.parent as ThingWithComps)?.AllComps
                    .FirstOrDefault(x => x is IHoldMultipleThings.IHoldMultipleThings) == null)
                    return true;

                var workingThingList = c.GetThingList(pawn.Map);
                var origThingList = new List<Thing>(workingThingList);

                var origPositions = new Dictionary<Thing, IntVec3>();
                var TPeverything = new TargetingParameters
                {
                    canTargetBuildings = false,
                    canTargetItems = false,
                    canTargetFires = true,
                    canTargetPawns = true,
                    canTargetSelf = true
                };
                foreach (var localTargetInfo in GenUI.TargetsAt_NewTemp(clickPosition, TPeverything))
                {
                    if (localTargetInfo.Thing == null)
                    {
                        Log.Warning("LWM.DeepStorage: got null target but should only have things?");
                        continue;
                    }

                    origPositions.Add(localTargetInfo.Thing, localTargetInfo.Thing.Position);
                    Patch_FloatMenuMakerMap.SetPosition(localTargetInfo.Thing, IntVec3.Invalid);
                }

                /*****************  Do magic ****************/
                var origParams = new object[] {clickPosition, pawn, opts};
                foreach (var k in origPositions)
                {
                    Patch_FloatMenuMakerMap.SetPosition(k.Key, k.Value);
                    // AHlO.Invoke(null, origParams);
                    Patch_FloatMenuMakerMap.SetPosition(k.Key, IntVec3.Invalid);
                }

                foreach (var t in origThingList)
                {
                    workingThingList.Add(t);
                    // AHlO.Invoke(null, origParams);
                    workingThingList.Remove(t);
                }

                /************ Cleanup: Put everything back! ***********/
                workingThingList.Clear();
                workingThingList.AddRange(origThingList);
                foreach (var t in origPositions) Patch_FloatMenuMakerMap.SetPosition(t.Key, t.Value);

                if (!Find.WindowStack.IsOpen(typeof(DSGUI_ListModal)))
                    Find.WindowStack.Add(new DSGUI_ListModal(pawn, origThingList, clickPosition, opts));

                return false;
            }
        }
    }
}