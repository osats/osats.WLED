using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace osats.WLED
{
    public sealed class WLEDHelper : IDisposable
    {
        private readonly UdpClient client = new UdpClient();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Func<long> nowMilliseconds;
        private bool previousGreen;
        private long greenUntil, testUntil, effectStart, lastSend = -10000, retryAfter;
        private string testKey, activeKey;
        private byte[] previousFrame;
        public string Status { get; private set; } = "À espera de telemetria";

        public WLEDHelper(Func<long> nowMilliseconds = null)
        {
            this.nowMilliseconds = nowMilliseconds ?? (() => clock.ElapsedMilliseconds);
        }

        public void Test(string key, int durationMs)
        {
            testKey = key;
            testUntil = nowMilliseconds() + durationMs;
            lastSend = -10000;
        }

        public void StopTest() { testKey = null; lastSend = -10000; }

        public void Update(WLEDSettings s, bool gameRunning, double rpm, double maxRpm,
            bool shouldShift, bool pitLimiter, bool yellowFlag, bool blueFlag, bool greenFlag, bool gameInMenu, double speed)
        {
            long now = nowMilliseconds();
            bool driving = gameRunning && !gameInMenu;
            if (driving && greenFlag && !previousGreen) greenUntil = now + s.GreenDurationMs;
            previousGreen = driving && greenFlag;
            if (!driving) greenUntil = 0;
            bool testing = testKey != null && now < testUntil;
            EffectOptions effect;
            if (testing) effect = s.Effects.FirstOrDefault(x => x.Key == testKey);
            else {
                testKey = null;
                effect = s.Effects.Where(x => x.Enabled && (
                    x.Key == (driving ? "driving" : "default") ||
                    driving && (x.Key == "pit" && pitLimiter || x.Key == "yellow" && yellowFlag ||
                        x.Key == "blue" && blueFlag || x.Key == "green" && now < greenUntil ||
                        x.Key == "shift" && shouldShift || x.Key == "rpm" && maxRpm > 0)))
                    .OrderByDescending(x => x.Priority).ThenBy(x => x.Key, StringComparer.Ordinal).FirstOrDefault();
            }
            string key = effect == null ? "off" : effect.Key;
            string stateKey = (testing ? "test:" : "live:") + key;
            if (stateKey != activeKey) { activeKey = stateKey; effectStart = now; lastSend = -10000; }
            if (now < retryAfter || now - lastSend < 1000.0 / s.FramesPerSecond) return;
            bool on = effect != null && (!effect.Blink || (now - effectStart) / effect.IntervalMs % 2 == 0);
            double fraction = testing ? 0.75 : RpmFraction(rpm, maxRpm, s.RpmStartPercent, s.RpmEndPercent);
            byte[] frame = BuildFrame(s, effect, on, fraction);
            // The one-second realtime timeout needs a periodic keepalive.
            if (previousFrame != null && frame.SequenceEqual(previousFrame) && now - lastSend < 500) return;
            try {
                client.Send(frame, frame.Length, new IPEndPoint(IPAddress.Parse(s.stripUrl), s.stripPort));
                lastSend = now;
                previousFrame = frame;
                Status = (testing ? "Teste: " : "Ativo: ") + (effect == null ? "Desligado" : effect.Name) + " · UDP enviado (sem confirmação)";
            }
            catch (SocketException ex) { Failed(now, ex); }
            catch (ArgumentException ex) { Failed(now, ex); }
        }

        private void Failed(long now, Exception ex)
        {
            retryAfter = now + 2000;
            Status = "Erro UDP: " + ex.Message;
            SimHub.Logging.Current.Warn("WLED: " + ex.Message);
        }

        public static double RpmFraction(double rpm, double maxRpm, double start, double end)
        {
            if (maxRpm <= 0 || double.IsNaN(rpm) || double.IsInfinity(rpm) || double.IsNaN(maxRpm) || double.IsInfinity(maxRpm)) return 0;
            return Math.Max(0, Math.Min(1, (100 * rpm / maxRpm - start) / (end - start)));
        }

        public static byte[] BuildFrame(WLEDSettings s, EffectOptions effect, bool on, double fraction)
        {
            var frame = new byte[2 + s.ledAmount * 4];
            frame[0] = 1; frame[1] = 1;
            int lit = (int)Math.Round(Math.Max(0, Math.Min(1, fraction)) * s.ledAmount);
            int start = s.center ? (s.ledAmount - lit) / 2 : 0;
            double brightness = effect == null ? 0 : s.Brightness * effect.Brightness / 10000.0;
            for (int i = 0; i < s.ledAmount; i++) {
                bool illuminated = on;
                if (effect != null && effect.Key == "rpm") {
                    if (s.center && s.mirror) {
                        int dark = s.ledAmount - lit;
                        int darkStart = (s.ledAmount - dark) / 2;
                        illuminated &= i < darkStart || i >= darkStart + dark;
                    }
                    else if (s.center) illuminated &= i >= start && i < start + lit;
                    else illuminated &= s.mirror ? i >= s.ledAmount - lit : i < lit;
                }
                int index = 2 + i * 4;
                frame[index] = (byte)(s.offset + i);
                if (illuminated && effect != null) {
                    frame[index + 1] = (byte)Math.Round(effect.R * brightness);
                    frame[index + 2] = (byte)Math.Round(effect.G * brightness);
                    frame[index + 3] = (byte)Math.Round(effect.B * brightness);
                }
            }
            return frame;
        }

        public void Dispose() { client.Dispose(); }
    }
}
