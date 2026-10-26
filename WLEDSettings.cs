namespace osats.WLED
{
    public partial class WLEDSettings
    {
        // Ligação
        public string stripUrl = "192.168.1.100";
        public int stripPort = 19446;
        public int ledAmount = 29;
        public int offset = 0;
        public bool mirror = false;
        public bool center = false;

        // RPM
        public bool rpmEnabled = true;
        public int rpmColorR = 0;
        public int rpmColorG = 0;
        public int rpmColorB = 255;
        public int rpmShiftColorR = 255;
        public int rpmShiftColorG = 0;
        public int rpmShiftColorB = 0;

        // Default (menus/fora de jogo)
        public bool defaultEnabled = true;
        public int defaultColorR = 255;
        public int defaultColorG = 0;
        public int defaultColorB = 0;

        // Running
        public bool drivingEnabled = true;
        public int drivingColorR = 0;
        public int drivingColorG = 50;
        public int drivingColorB = 150;

        // Green flag
        public bool greenFlagEnabled = true;
        public int greenFlagColorR = 0;
        public int greenFlagColorG = 255;
        public int greenFlagColorB = 0;

        // Yellow Flag
        public bool yellowFlagEnabled = true;
        public int yellowFlagColorR = 255;
        public int yellowFlagColorG = 200;
        public int yellowFlagColorB = 0;

        // Blue Flag
        public bool blueFlagEnabled = true;
        public int blueFlagColorR = 0;
        public int blueFlagColorG = 100;
        public int blueFlagColorB = 255;

        // Pit limiter
        public bool pitLimiterEnabled = true;
        public int pitLimiterColorR = 255;
        public int pitLimiterColorG = 100;
        public int pitLimiterColorB = 0;
    }
}
