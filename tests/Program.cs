using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml.Serialization;
using osats.WLED;

// Isolate the pure engine tests from the SimHub runtime.
namespace SimHub.Logging {
    public static class Current { public static void Warn(string message) { Console.WriteLine(message); } }
}

static class Program
{
    static int checks;
    static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
    static void Invalid(Action action, string name) {
        try { action(); } catch (ArgumentException) { checks++; return; }
        throw new Exception("Accepted invalid value: " + name);
    }
    static int Lit(byte[] frame) {
        int count = 0;
        for (int i = 2; i < frame.Length; i += 4) if (frame[i+1] + frame[i+2] + frame[i+3] > 0) count++;
        return count;
    }
    static void Main()
    {
        var s = new WLEDSettings(); s.Validate();
        Check(s.Effects.Count == 8, "migrate legacy effects");
        var legacy = new WLEDSettings { rpmColorR = 123, yellowFlagEnabled = false };
        legacy.Migrate();
        Check(legacy.Effects.Single(x => x.Key == "rpm").R == 123 && !legacy.Effects.Single(x => x.Key == "yellow").Enabled, "legacy values preserved");
        var rpm = s.Effects.Single(x => x.Key == "rpm");
        foreach (int length in new[] { 1, 2, 29, 30, 256 })
        foreach (bool center in new[] { false, true })
        foreach (bool mirror in new[] { false, true })
        foreach (double fraction in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }) {
            s.ledAmount = length; s.offset = 256 - length; s.center = center; s.mirror = mirror;
            var frame = WLEDHelper.BuildFrame(s, rpm, true, fraction);
            Check(frame.Length == 2 + 4 * length && frame[0] == 1 && frame[1] == 1, "packet shape");
            Check(Lit(frame) == (int)Math.Round(length * fraction), "exact lit count for odd/even strips");
            Check(frame[2] == s.offset && frame[frame.Length-4] == 255, "offset and final index");
        }
        s = new WLEDSettings(); s.Validate();
        Check(WLEDHelper.RpmFraction(7500, 10000, 50, 100) == 0.5, "RPM range");
        Check(WLEDHelper.RpmFraction(double.NaN, 10000, 50, 100) == 0, "NaN RPM");
        s.offset = 250; Invalid(() => s.Validate(), "index overflow"); s.offset = 0;
        s.Effects[0].R = 256; Invalid(() => s.Validate(), "RGB overflow"); s.Effects[0].R = 255;
        s.RpmStartPercent = double.NaN; Invalid(() => s.Validate(), "NaN threshold"); s.RpmStartPercent = 50;
        s.stripUrl = "not-an-ip"; Invalid(() => s.Validate(), "IP"); s.stripUrl = "127.0.0.1";
        var serializer = new XmlSerializer(typeof(WLEDSettings));
        using (var stream = new MemoryStream()) {
            serializer.Serialize(stream, s); stream.Position = 0;
            var loaded = (WLEDSettings)serializer.Deserialize(stream); loaded.Validate();
            Check(loaded.Effects.Count == 8 && loaded.Brightness == s.Brightness, "profile roundtrip");
        }
        using (var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0))) {
            receiver.Client.ReceiveTimeout = 1000;
            s.stripPort = ((IPEndPoint)receiver.Client.LocalEndPoint).Port;
            long now = 0;
            using (var helper = new WLEDHelper(() => now)) {
                Action<bool, bool, bool, bool, bool> update = (green, yellow, pit, running, menu) => helper.Update(s, running, 7500, 10000, false, pit, yellow, false, green, menu, 0);
                Func<byte[]> receive = () => { IPEndPoint ep = null; return receiver.Receive(ref ep); };
                update(true, false, false, true, false); receive();
                Check(helper.Status.Contains("Bandeira verde"), "green begins at standstill");
                now = 5100; update(true, false, false, true, false); receive();
                Check(helper.Status.Contains("Barra de RPM"), "held green does not retrigger");
                now = 5200; update(false, true, true, true, false); receive();
                Check(helper.Status.Contains("Limitador"), "pit priority at standstill");
                now = 5300; update(false, true, true, true, true); receive();
                Check(helper.Status.Contains("Menus"), "menu overrides telemetry");
                now = 5400; update(false, false, false, false, false);
                helper.Test("yellow", 1000);
                update(false, false, false, false, false); var first = receive();
                Check(helper.Status.Contains("Teste:") && Lit(first) == 29, "test without game begins on");
                now = 6500; update(false, false, false, false, false); receive();
                Check(!helper.Status.Contains("Teste:"), "test expires");
                now = 6600; update(false, false, false, false, false);
                Check(receiver.Available == 0, "unchanged frame suppressed");
                now = 7100; update(false, false, false, false, false); receive();
                Check(helper.Status.Contains("UDP enviado"), "keepalive");
            }
        }
        Console.WriteLine("PASS: " + checks + " assertions");
    }
}
