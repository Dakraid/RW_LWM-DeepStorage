using HugsLib;
using HugsLib.Settings;
using Verse;

namespace LWM.DeepStorage
{
    internal class LWM_Hug : ModBase
    {
#if DEBUG
        private readonly SettingHandle<bool>[] debugONorOFF = new SettingHandle<bool>[Utils.showDebug.Length];
#endif
        public override string ModIdentifier => "LWM_DeepStorage";
        public override void DefsLoaded()
        {
#if DEBUG
            Log.Message("LWM.DeepStorage:  DefsLoaded via HugsLib():");
            for (var i = 1; i < Utils.showDebug.Length; i++)
                debugONorOFF[i] = Settings.GetHandle("turnDebugONorOFF" + (Utils.DBF) i, "Turn ON/OFF debugging: " + (Utils.DBF) i,
                    "Turn ON/OFF all debugging - this is a lot of trace, and only available on debug builds",
                    false);
            SettingsChanged();
#endif
            DeepStorage.Settings.DefsLoaded();
        }
#if DEBUG
        public override void SettingsChanged()
        {
            Log.Message("LWM's Deep Storage: Debug settings changed");
            UpdateDebug();
        }

        public void UpdateDebug()
        {
            for (var i = 1; i < Utils.showDebug.Length; i++) // 0 is always true
                Utils.showDebug[i] = debugONorOFF[i];
        }
#endif
    }
}