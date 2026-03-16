using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace osats.WLED
{
    public class WLEDHelper
    {
        private string host;
        private int port;
        public int LedAmount { get; set; }
        public int Offset { get; set; }
        public bool Mirror { get; set; }
        public bool Center { get; set; }

        private UdpClient LedClient { get; set; }

        // estado green flag
        private DateTime greenFlagStart = DateTime.MinValue;
        private bool greenFlagActive = false;

        // estado piscar
        private DateTime lastBlink = DateTime.MinValue;
        private bool blinkState = false;

        public WLEDHelper(string host, int port)
        {
            this.host = host;
            this.port = port;
            LedClient = new UdpClient();
        }

        public void Update(WLEDSettings s, bool gameRunning, double rpm, double maxRpm,
            bool shouldShift, bool pitLimiter, bool yellowFlag, bool blueFlag, bool greenFlag, bool gameInMenu, double speed)
        {
            (int, int, int) color;
            bool doBlink = false;
            double blinkInterval = 500;

            // green flag — activa timer
            if (greenFlag && !greenFlagActive)
            {
                greenFlagActive = true;
                greenFlagStart = DateTime.Now;
            }
            bool greenFlagRunning = greenFlagActive &&
                (DateTime.Now - greenFlagStart).TotalSeconds < 5;
            if (!greenFlagRunning) greenFlagActive = false;

            // --- prioridade ---
            if (!gameRunning || speed < 10)
            {
                if (!s.defaultEnabled) return;
                color = (s.defaultColorR, s.defaultColorG, s.defaultColorB);
                doBlink = false;
            }
            else if (pitLimiter && s.pitLimiterEnabled)
            {
                color = (s.pitLimiterColorR, s.pitLimiterColorG, s.pitLimiterColorB);
                doBlink = true;
                blinkInterval = 200;
            }
            else if (yellowFlag && s.yellowFlagEnabled)
            {
                color = (s.yellowFlagColorR, s.yellowFlagColorG, s.yellowFlagColorB);
                doBlink = true;
                blinkInterval = 600;
            }
            else if (blueFlag && s.blueFlagEnabled)
            {
                color = (s.blueFlagColorR, s.blueFlagColorG, s.blueFlagColorB);
                doBlink = true;
                blinkInterval = 600;
            }
            else if (greenFlagRunning && s.greenFlagEnabled)
            {
                color = (s.greenFlagColorR, s.greenFlagColorG, s.greenFlagColorB);
                doBlink = true;
                blinkInterval = 150;
            }
            else if (s.rpmEnabled && maxRpm > 0)
            {
                ShowRpm(s, rpm, maxRpm, shouldShift);
                return;
            }
            else if (s.drivingEnabled)
            {
                color = (s.drivingColorR, s.drivingColorG, s.drivingColorB);
                doBlink = false;
            }
            else return;

            // piscar
            if (doBlink)
            {
                if ((DateTime.Now - lastBlink).TotalMilliseconds > blinkInterval)
                {
                    blinkState = !blinkState;
                    lastBlink = DateTime.Now;
                }
                if (!blinkState) color = (0, 0, 0);
            }

            SendSolidColor(color);
        }

        private void ShowRpm(WLEDSettings s, double rpm, double maxRpm, bool shouldShift)
        {
            if (maxRpm <= 0) return;
            int scaledAmount = (int)(rpm / (maxRpm / LedAmount));
            scaledAmount = Math.Max(0, Math.Min(scaledAmount, LedAmount));

            int left = 0 + Offset;
            int right = LedAmount + Offset;

            (int, int, int) riseColor = (s.rpmColorR, s.rpmColorG, s.rpmColorB);
            (int, int, int) shiftColor = (s.rpmShiftColorR, s.rpmShiftColorG, s.rpmShiftColorB);
            (int, int, int) offColor = (0, 0, 0);

            (int, int, int) innerColor = Mirror ? offColor : riseColor;
            (int, int, int) outerColor = Mirror ? riseColor : offColor;

            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);

            if (shouldShift)
            {
                for (int i = left; i < right; i++)
                {
                    data.Add(BitConverter.GetBytes(i)[0]);
                    data.AddRange(ColorToBytes(shiftColor));
                }
            }
            else
            {
                if (Center)
                {
                    var center = right / 2;
                    int leftBorder = center - scaledAmount / 2;
                    int rightBorder = center + scaledAmount / 2;
                    if (Mirror) { leftBorder = left + scaledAmount / 2; rightBorder = right - scaledAmount / 2; }
                    for (int i = left; i < leftBorder; i++) { data.Add(BitConverter.GetBytes(i)[0]); data.AddRange(ColorToBytes(outerColor)); }
                    for (int i = leftBorder; i < rightBorder; i++) { data.Add(BitConverter.GetBytes(i)[0]); data.AddRange(ColorToBytes(innerColor)); }
                    for (int i = rightBorder; i < right; i++) { data.Add(BitConverter.GetBytes(i)[0]); data.AddRange(ColorToBytes(outerColor)); }
                }
                else
                {
                    var border = left + scaledAmount;
                    if (Mirror) border = right - scaledAmount;
                    for (int i = left; i < border; i++) { data.Add(BitConverter.GetBytes(i)[0]); data.AddRange(ColorToBytes(innerColor)); }
                    for (int i = border; i < right; i++) { data.Add(BitConverter.GetBytes(i)[0]); data.AddRange(ColorToBytes(outerColor)); }
                }
            }

            var arr = data.ToArray();
            var ep = new IPEndPoint(IPAddress.Parse(host), port);
            LedClient.Send(arr, arr.Length, ep);
        }

        private void SendSolidColor((int, int, int) color)
        {
            int left = Offset;
            int right = LedAmount + Offset;
            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);
            for (int i = left; i < right; i++)
            {
                data.Add(BitConverter.GetBytes(i)[0]);
                data.AddRange(ColorToBytes(color));
            }
            var arr = data.ToArray();
            var ep = new IPEndPoint(IPAddress.Parse(host), port);
            LedClient.Send(arr, arr.Length, ep);
        }

        private List<byte> ColorToBytes((int, int, int) color)
        {
            return new List<byte> {
                BitConverter.GetBytes(color.Item1)[0],
                BitConverter.GetBytes(color.Item2)[0],
                BitConverter.GetBytes(color.Item3)[0]
            };
        }
    }
}
