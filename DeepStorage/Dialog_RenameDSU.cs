using RimWorld;
using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    // ripped shamelessly from Dialog_RenameZone
    public class Dialog_RenameDSU : Dialog_Rename
    {
        // TODO:  Make a nice button, eh?
        // override InitialSize to make it bigger
        // override DoWindowContents to add a new button on top of "Okay" that says "reset"?

        private readonly CompDeepStorage cds;

        public Dialog_RenameDSU(CompDeepStorage cds)
        {
            this.cds = cds;
            curName = cds.parent.Label;
        }

        public override Vector2 InitialSize
        {
            get
            {
                var o = base.InitialSize;
                o.y += 50f;
                return o;
            }
        }

        // ... Actually, whatever, name it whatever you want.
        // But use "" to reset to default.
        protected override AcceptanceReport NameIsValid(string name)
        {
            if (name.Length == 0) return true;

            var result = base.NameIsValid(name);
            return !result.Accepted ? result : true;
        }

        protected override void SetName(string name)
        {
            cds.buildingLabel = name;
            Messages.Message("LWM_DSU_GainsName".Translate(cds.parent.def.label, cds.parent.Label),
                MessageTypeDefOf.TaskCompletion, false);
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f - 50f, inRect.width - 15f - 15f, 35f), "ResetButton".Translate(),
                true, false))
            {
                SetName("");
                Find.WindowStack.TryRemove(this);
            }
        }
    }
}