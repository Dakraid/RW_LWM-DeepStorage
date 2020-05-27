// for OpCodes in Harmony Transpiler

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static LWM.DeepStorage.Utils.DBF; // trace utils

namespace LWM.DeepStorage
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "TryMakeFloatMenu")]
    internal static class Patch_TryMakeFloatMenu
    {
        private static bool Prepare(Harmony instance)
        {
            return Settings.useDeepStorageNewRightClick;
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn pawn)
        {
            return DSGUI.ContextMenuStorage.Create(UI.MouseMapPosition(), pawn);
        }
    }

    [HarmonyPatch(typeof(GridsUtility), "GetThingList")]
    internal static class Patch_GetThingList
    {
        private static bool Prepare(Harmony instance)
        {
            return Settings.useDeepStorageNewRightClick;
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ref List<Thing> __result)
        {
            if (DSGUI.GlobalStorage.currThing == null)
                return true;

            DSGUI.GlobalStorage.lastThing = DSGUI.GlobalStorage.currThing;
            __result = new List<Thing> {DSGUI.GlobalStorage.currThing};
            
            return false;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref List<Thing> __result)
        {
            if (DSGUI.GlobalStorage.currThing == null)
                return;
            
            Log.Warning("[LWM] GetThingList returned `" + __result.ToStringSafeEnumerable() + "`");
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    internal static class Patch_AddHumanlikeOrders
    {
        private static bool Prepare(Harmony instance)
        {
            Utils.Warn(RightClickMenu, "Loading AddHumanlikeOrders menu code: " + Settings.useDeepStorageRightClickLogic);
            return Settings.useDeepStorageRightClickLogic;
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (DSGUI.GlobalStorage.currThing == null)
                return true;

            return DSGUI.GlobalStorage.currThing != DSGUI.GlobalStorage.lastThing;

            // return Patch_FloatMenuMakerMap.OldContextMenu(clickPos, IntVec3.Invalid, pawn, opts);
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            // Patch_FloatMenuMakerMap.OldPostfix(clickPos, pawn, opts);
        }
    }

    internal static class Patch_FloatMenuMakerMap
    {
        private static bool runningPatchLogic;
        private static readonly List<FloatMenuOption> realList = new List<FloatMenuOption>();
        private static int failsafe;

        // Allow calling AddHumanlikeOrders
        private static readonly MethodInfo AHlO = AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders");

        // Allow directly setting Position of things.  And setting it back.
        private static readonly FieldInfo fieldPosition = typeof(Thing).GetField("positionInt",
            BindingFlags.Instance |
            BindingFlags.GetField |
            BindingFlags.SetField |
            BindingFlags.NonPublic);

        // Original LWM DeepStorage Right Click
        // We have to run as Prefix, because we need to intercept the incoming List.
        public static bool OldContextMenu(Vector3 clickPosition, IntVec3 c, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (failsafe++ > 500) runningPatchLogic = false;
            if (runningPatchLogic) return true;

            // Only give nice tidy menu if items are actually in Deep Storage: otherwise, they
            //   are a jumbled mess on the floor, and pawns can only interact with what's on
            //   top until they've cleaned up the mess.
            // I *could* do better and throw away all items below, but whatev's this is good enuf.
            c = IntVec3.FromVector3(clickPosition);

            if ((c.GetSlotGroup(pawn.Map)?.parent as ThingWithComps)?.AllComps
                .FirstOrDefault(x => x is IHoldMultipleThings.IHoldMultipleThings) == null)
            {
                Utils.Warn(RightClickMenu, "Location " + c + " is not in any DSU; continuing.");
                return true; // out of luck, so sorry!
                // Note: also need to handle this case in Postfix!
            }

            failsafe = 0;

            Utils.Err(RightClickMenu, "Testing Location " + c);

            runningPatchLogic = true;

            // TODO: get default set of menus and tidy them away somehow?  This seems to be unnecessary so far.
            // ThingsListAt:
            var workingThingList = c.GetThingList(pawn.Map);
            var origThingList = new List<Thing>(workingThingList);
            workingThingList.Clear();
            // ...other ...things.
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
                Utils.Warn(RightClickMenu, "Adding position information for LocalTargetInfo "
                                           + localTargetInfo.Thing);
                SetPosition(localTargetInfo.Thing, IntVec3.Invalid);
            }

            var origParams = new object[] {clickPosition, pawn, opts};
            foreach (var k in origPositions)
            {
                SetPosition(k.Key, k.Value);
                Utils.Mess(RightClickMenu, "  Doing Menu for Target " + k.Key);
                AHlO.Invoke(null, origParams);
                //showOpts(opts);
                SetPosition(k.Key, IntVec3.Invalid);
            }

            foreach (var t in origThingList)
            {
                workingThingList.Add(t);
                Utils.Mess(RightClickMenu, "  Doing Menu for Item " + t);
                AHlO.Invoke(null, origParams);
                //showOpts(opts);
                workingThingList.Remove(t);
            }

            workingThingList.Clear();
            workingThingList.AddRange(origThingList);
            foreach (var t in origPositions) SetPosition(t.Key, t.Value);
            runningPatchLogic = false;

            realList.Clear();
            foreach (var m in opts) realList.Add(m); // got to store it in case anything adjusts it in a different Postfix
            return false;
        }

        public static void OldPostfix(Vector3 clickPosition, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (runningPatchLogic) return;
            if (realList.Count == 0) return; // incidentally breaks out of logic here in case not in a DSU

            opts.Clear();
            opts.AddRange(realList);
            realList.Clear();
        }

        // Allow directly setting Position of things.  And setting it back.
        private static void SetPosition(Thing t, IntVec3 p)
        {
            fieldPosition.SetValue(t, p);
        }
    }
}