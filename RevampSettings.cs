using System;
using System.Collections.Generic;
using System.Net;

namespace osats.WLED
{
    public class EffectOptions
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int Brightness { get; set; } = 100;
        public bool Blink { get; set; }
        public int IntervalMs { get; set; } = 500;
        public int Priority { get; set; }
        public void Validate()
        {
            if (R < 0 || R > 255 || G < 0 || G > 255 || B < 0 || B > 255 ||
                Brightness < 0 || Brightness > 100 || IntervalMs < 50 || IntervalMs > 10000 || Priority < 0 || Priority > 100)
                throw new ArgumentException("Cores: 0–255; brilho/prioridade: 0–100; intervalo: 50–10000 ms.");
        }
    }

    public partial class WLEDSettings
    {
        public int SchemaVersion = 2;
        public int Brightness = 60;
        public int FramesPerSecond = 30;
        public int GreenDurationMs = 5000;
        public int TestDurationMs = 3000;
        public double RpmStartPercent = 50;
        public double RpmEndPercent = 100;
        public List<EffectOptions> Effects;

        public void Migrate()
        {
            if (Effects != null) return;
            Effects = new List<EffectOptions> {
                Effect("default", "Menus / sem jogo", defaultEnabled, defaultColorR, defaultColorG, defaultColorB, false, 500, 0),
                Effect("driving", "A conduzir", drivingEnabled, drivingColorR, drivingColorG, drivingColorB, false, 500, 0),
                Effect("rpm", "Barra de RPM", rpmEnabled, rpmColorR, rpmColorG, rpmColorB, false, 500, 10),
                Effect("shift", "Trocar de mudança", true, rpmShiftColorR, rpmShiftColorG, rpmShiftColorB, true, 100, 20),
                Effect("green", "Bandeira verde", greenFlagEnabled, greenFlagColorR, greenFlagColorG, greenFlagColorB, true, 150, 30),
                Effect("blue", "Bandeira azul", blueFlagEnabled, blueFlagColorR, blueFlagColorG, blueFlagColorB, true, 600, 40),
                Effect("yellow", "Bandeira amarela", yellowFlagEnabled, yellowFlagColorR, yellowFlagColorG, yellowFlagColorB, true, 600, 50),
                Effect("pit", "Limitador das boxes", pitLimiterEnabled, pitLimiterColorR, pitLimiterColorG, pitLimiterColorB, true, 200, 60)
            };
        }

        private static EffectOptions Effect(string key, string name, bool enabled, int r, int g, int b, bool blink, int interval, int priority)
            => new EffectOptions { Key = key, Name = name, Enabled = enabled, R = r, G = g, B = b, Blink = blink, IntervalMs = interval, Priority = priority };

        public void Validate()
        {
            IPAddress ip;
            if (!IPAddress.TryParse(stripUrl, out ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Introduz um endereço IPv4 válido para o WLED.");
            if (stripPort < 1 || stripPort > 65535 || ledAmount < 1 || offset < 0 || offset + (long)ledAmount > 256)
                throw new ArgumentException("Porta: 1–65535. A zona de LEDs deve ficar entre os índices 0 e 255.");
            if (Brightness < 0 || Brightness > 100 || FramesPerSecond < 1 || FramesPerSecond > 60 ||
                GreenDurationMs < 100 || GreenDurationMs > 60000 || TestDurationMs < 100 || TestDurationMs > 10000 ||
                double.IsNaN(RpmStartPercent) || double.IsNaN(RpmEndPercent) || RpmStartPercent < 0 || RpmEndPercent > 100 || RpmStartPercent >= RpmEndPercent)
                throw new ArgumentException("Verifica brilho, FPS, durações e intervalo de RPM (0–100%, início < fim).");
            Migrate();
            var expected = new WLEDSettings(); expected.Migrate();
            if (Effects.Count != expected.Effects.Count) throw new ArgumentException("Perfil com efeitos em falta ou duplicados.");
            var keys = new HashSet<string>();
            foreach (var effect in Effects) {
                if (effect == null || !keys.Add(effect.Key) || !expected.Effects.Exists(x => x.Key == effect.Key))
                    throw new ArgumentException("Perfil com efeitos desconhecidos ou duplicados.");
                effect.Validate();
            }
        }
    }
}
