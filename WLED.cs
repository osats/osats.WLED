using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace osats.WLED
{
    [PluginDescription("WLED things")]
    [PluginAuthor("osats")]
    [PluginName("WLED")]
    public class WLED : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public WLEDSettings Settings;

        private WLEDHelper LedHelper { get; set; }

        public PluginManager PluginManager { get; set; }

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

        public string LeftMenuTitle => "WLED";

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            LedHelper.LedAmount = Settings.ledAmount;
            LedHelper.Offset = Settings.offset;
            LedHelper.Mirror = Settings.mirror;
            LedHelper.Center = Settings.center;

            bool gameInMenu = data.GameInMenu;
            bool gameRunning = data.GameRunning;
            double speed = gameRunning ? data.NewData.SpeedKmh : 0;
            double rpm = gameRunning ? data.NewData.Rpms : 0;
            double maxRpm = gameRunning ? data.NewData.MaxRpm : 1;
            bool shouldShift = gameRunning && data.NewData.CarSettings_RPMShiftLight1 == 1.0;
            bool pitLimiter = gameRunning && data.NewData.PitLimiterOn>0;
            bool yellowFlag = gameRunning && data.NewData.Flag_Yellow > 0;
            bool blueFlag = gameRunning && data.NewData.Flag_Blue > 0;
            bool greenFlag = gameRunning && data.NewData.Flag_Green > 0;

            LedHelper.Update(Settings, gameRunning, rpm, maxRpm,
                shouldShift, pitLimiter, yellowFlag, blueFlag, greenFlag, gameInMenu, speed);
        }

        public void End(PluginManager pluginManager)
        {
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting WLED plugin");
            Settings = this.ReadCommonSettings<WLEDSettings>("GeneralSettings", () => new WLEDSettings());
            LedHelper = new WLEDHelper(Settings.stripUrl, Settings.stripPort);
            LedHelper.LedAmount = Settings.ledAmount;
        }

        public void ReinitHelper()
        {
            LedHelper = new WLEDHelper(Settings.stripUrl, Settings.stripPort);
            LedHelper.LedAmount = Settings.ledAmount;
        }
    }
}
