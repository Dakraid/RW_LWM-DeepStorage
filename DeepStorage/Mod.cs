using UnityEngine;
using Verse;

namespace LWM.DeepStorage
{
    public class DeepStorageMod : Mod
    {
        private Settings settings;

        public DeepStorageMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "LWM's Deep Storage"; // todo: translate?
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }
    }
}