using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace osats.WLED
{
    public partial class SettingsControl : UserControl
    {
        public WLED Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(WLED plugin) : this()
        {
            this.Plugin = plugin;
            var s = Plugin.Settings;

            // Ligação
            UrlInput.Text = s.stripUrl;
            PortInput.Text = s.stripPort.ToString();
            LedAmount.Text = s.ledAmount.ToString();
            LedOffset.Text = s.offset.ToString();
            Mirror.IsChecked = s.mirror;
            Center.IsChecked = s.center;

            // Effects
            DefaultEnabled.IsChecked = s.defaultEnabled;
            DefaultR.Text = s.defaultColorR.ToString();
            DefaultG.Text = s.defaultColorG.ToString();
            DefaultB.Text = s.defaultColorB.ToString();

            DrivingEnabled.IsChecked = s.drivingEnabled;
            DrivingR.Text = s.drivingColorR.ToString();
            DrivingG.Text = s.drivingColorG.ToString();
            DrivingB.Text = s.drivingColorB.ToString();

            RpmEnabled.IsChecked = s.rpmEnabled;
            RpmR.Text = s.rpmColorR.ToString();
            RpmG.Text = s.rpmColorG.ToString();
            RpmB.Text = s.rpmColorB.ToString();

            GreenFlagEnabled.IsChecked = s.greenFlagEnabled;
            GreenFlagR.Text = s.greenFlagColorR.ToString();
            GreenFlagG.Text = s.greenFlagColorG.ToString();
            GreenFlagB.Text = s.greenFlagColorB.ToString();

            YellowFlagEnabled.IsChecked = s.yellowFlagEnabled;
            YellowFlagR.Text = s.yellowFlagColorR.ToString();
            YellowFlagG.Text = s.yellowFlagColorG.ToString();
            YellowFlagB.Text = s.yellowFlagColorB.ToString();

            BlueFlagEnabled.IsChecked = s.blueFlagEnabled;
            BlueFlagR.Text = s.blueFlagColorR.ToString();
            BlueFlagG.Text = s.blueFlagColorG.ToString();
            BlueFlagB.Text = s.blueFlagColorB.ToString();

            PitLimiterEnabled.IsChecked = s.pitLimiterEnabled;
            PitLimiterR.Text = s.pitLimiterColorR.ToString();
            PitLimiterG.Text = s.pitLimiterColorG.ToString();
            PitLimiterB.Text = s.pitLimiterColorB.ToString();

            // Actualiza previews iniciais
            RefreshAllPreviews();
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            SimHub.Logging.Current.Info("Apply WLED Settings");
            var s = Plugin.Settings;

            s.stripUrl = UrlInput.Text.Trim();
            s.stripPort = ParseInt(PortInput.Text, 19446);
            s.ledAmount = ParseInt(LedAmount.Text, 29);
            s.offset = ParseInt(LedOffset.Text, 0);
            s.mirror = Mirror.IsChecked == true;
            s.center = Center.IsChecked == true;

            s.defaultEnabled = DefaultEnabled.IsChecked == true;
            s.defaultColorR = ParseInt(DefaultR.Text, 255);
            s.defaultColorG = ParseInt(DefaultG.Text, 0);
            s.defaultColorB = ParseInt(DefaultB.Text, 0);

            s.drivingEnabled = DrivingEnabled.IsChecked == true;
            s.drivingColorR = ParseInt(DrivingR.Text, 0);
            s.drivingColorG = ParseInt(DrivingG.Text, 50);
            s.drivingColorB = ParseInt(DrivingB.Text, 150);

            s.rpmEnabled = RpmEnabled.IsChecked == true;
            s.rpmColorR = ParseInt(RpmR.Text, 0);
            s.rpmColorG = ParseInt(RpmG.Text, 0);
            s.rpmColorB = ParseInt(RpmB.Text, 255);

            s.greenFlagEnabled = GreenFlagEnabled.IsChecked == true;
            s.greenFlagColorR = ParseInt(GreenFlagR.Text, 0);
            s.greenFlagColorG = ParseInt(GreenFlagG.Text, 255);
            s.greenFlagColorB = ParseInt(GreenFlagB.Text, 0);

            s.yellowFlagEnabled = YellowFlagEnabled.IsChecked == true;
            s.yellowFlagColorR = ParseInt(YellowFlagR.Text, 255);
            s.yellowFlagColorG = ParseInt(YellowFlagG.Text, 200);
            s.yellowFlagColorB = ParseInt(YellowFlagB.Text, 0);

            s.blueFlagEnabled = BlueFlagEnabled.IsChecked == true;
            s.blueFlagColorR = ParseInt(BlueFlagR.Text, 0);
            s.blueFlagColorG = ParseInt(BlueFlagG.Text, 100);
            s.blueFlagColorB = ParseInt(BlueFlagB.Text, 255);

            s.pitLimiterEnabled = PitLimiterEnabled.IsChecked == true;
            s.pitLimiterColorR = ParseInt(PitLimiterR.Text, 255);
            s.pitLimiterColorG = ParseInt(PitLimiterG.Text, 100);
            s.pitLimiterColorB = ParseInt(PitLimiterB.Text, 0);

            // Recriar helper com novo host/port
            Plugin.ReinitHelper();
        }

        // --- Preview helpers ---

        private void UpdatePreview_Default(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewDefault, DefaultR, DefaultG, DefaultB);

        private void UpdatePreview_Driving(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewDriving, DrivingR, DrivingG, DrivingB);

        private void UpdatePreview_Rpm(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewRpm, RpmR, RpmG, RpmB);

        private void UpdatePreview_GreenFlag(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewGreenFlag, GreenFlagR, GreenFlagG, GreenFlagB);

        private void UpdatePreview_YellowFlag(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewYellowFlag, YellowFlagR, YellowFlagG, YellowFlagB);

        private void UpdatePreview_BlueFlag(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewBlueFlag, BlueFlagR, BlueFlagG, BlueFlagB);

        private void UpdatePreview_PitLimiter(object sender, TextChangedEventArgs e)
            => UpdatePreview(PreviewPitLimiter, PitLimiterR, PitLimiterG, PitLimiterB);

        private void UpdatePreview(Border preview, TextBox r, TextBox g, TextBox b)
        {
            try
            {
                int rv = ParseInt(r.Text, 0);
                int gv = ParseInt(g.Text, 0);
                int bv = ParseInt(b.Text, 0);
                rv = Math.Max(0, Math.Min(255, rv));
                gv = Math.Max(0, Math.Min(255, gv));
                bv = Math.Max(0, Math.Min(255, bv));
                preview.Background = new SolidColorBrush(Color.FromRgb((byte)rv, (byte)gv, (byte)bv));
            }
            catch { }
        }

        private void RefreshAllPreviews()
        {
            UpdatePreview(PreviewDefault, DefaultR, DefaultG, DefaultB);
            UpdatePreview(PreviewDriving, DrivingR, DrivingG, DrivingB);
            UpdatePreview(PreviewRpm, RpmR, RpmG, RpmB);
            UpdatePreview(PreviewGreenFlag, GreenFlagR, GreenFlagG, GreenFlagB);
            UpdatePreview(PreviewYellowFlag, YellowFlagR, YellowFlagG, YellowFlagB);
            UpdatePreview(PreviewBlueFlag, BlueFlagR, BlueFlagG, BlueFlagB);
            UpdatePreview(PreviewPitLimiter, PitLimiterR, PitLimiterG, PitLimiterB);
        }

        private int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, out int val) ? val : fallback;
        }
    }
}
