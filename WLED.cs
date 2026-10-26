using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace osats.WLED
{
    [PluginDescription("Configurable WLED telemetry lighting")]
    [PluginAuthor("osats")]
    [PluginName("WLED")]
    public class WLED : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        private readonly object gate = new object();
        public WLEDSettings Settings;
        private WLEDHelper helper;
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "WLED";
        public string Status { get { lock (gate) return helper == null ? "Parado" : helper.Status; } }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            lock (gate) {
                if (helper == null) return;
                bool running = data.GameRunning && data.NewData != null;
                helper.Update(Settings, running, running ? data.NewData.Rpms : 0, running ? data.NewData.MaxRpm : 0,
                    running && data.NewData.CarSettings_RPMShiftLight1 == 1.0,
                    running && data.NewData.PitLimiterOn > 0,
                    running && data.NewData.Flag_Yellow > 0, running && data.NewData.Flag_Blue > 0,
                    running && data.NewData.Flag_Green > 0, data.GameInMenu, running ? data.NewData.SpeedKmh : 0);
            }
        }

        public void ApplySettings(WLEDSettings settings)
        {
            settings.Validate();
            lock (gate) {
                this.SaveCommonSettings("GeneralSettings", settings);
                Settings = settings;
                helper?.Dispose();
                helper = new WLEDHelper();
            }
        }

        public void Test(string key) { lock (gate) helper?.Test(key, Settings.TestDurationMs); }
        public void StopTest() { lock (gate) helper?.StopTest(); }

        public void End(PluginManager pluginManager)
        {
            lock (gate) {
                this.SaveCommonSettings("GeneralSettings", Settings);
                helper?.Dispose(); helper = null;
            }
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) => new SettingsControl(this);

        public void Init(PluginManager pluginManager)
        {
            Settings = this.ReadCommonSettings<WLEDSettings>("GeneralSettings", () => new WLEDSettings());
            try { Settings.Validate(); }
            catch (ArgumentException ex) {
                SimHub.Logging.Current.Warn("WLED settings invalid; using defaults. " + ex.Message);
                Settings = new WLEDSettings(); Settings.Migrate();
            }
            helper = new WLEDHelper();
        }
    }
}
