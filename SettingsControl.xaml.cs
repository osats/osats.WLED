using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace osats.WLED
{
    public partial class SettingsControl : UserControl
    {
        private readonly WLED plugin;
        private WLEDSettings draft;
        private readonly List<Action> readers = new List<Action>();
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };

        public SettingsControl() { InitializeComponent(); }

        public SettingsControl(WLED plugin) : this()
        {
            this.plugin = plugin;
            draft = Clone(plugin.Settings);
            BuildEditor();
            timer.Tick += (s, e) => LiveStatus.Text = plugin.Status;
            Loaded += (s, e) => timer.Start();
            Unloaded += (s, e) => { timer.Stop(); plugin.StopTest(); };
        }

        private static WLEDSettings Clone(WLEDSettings settings)
        {
            var serializer = new XmlSerializer(typeof(WLEDSettings));
            using (var stream = new MemoryStream()) {
                serializer.Serialize(stream, settings);
                stream.Position = 0;
                return (WLEDSettings)serializer.Deserialize(stream);
            }
        }

        private StackPanel Section(string name)
        {
            var panel = new StackPanel();
            var expander = new Expander { Header = name, IsExpanded = true, Content = panel, Margin = new Thickness(0, 0, 0, 14) };
            Editor.Children.Add(expander);
            return panel;
        }

        private TextBox Field(Panel parent, string label, string value)
        {
            var row = new DockPanel { Margin = new Thickness(8, 4, 8, 4) };
            row.Children.Add(new TextBlock { Text = label, Width = 235, VerticalAlignment = VerticalAlignment.Center });
            var input = new TextBox { Text = value, MinWidth = 80, MaxWidth = 220, HorizontalAlignment = HorizontalAlignment.Left };
            row.Children.Add(input); parent.Children.Add(row);
            return input;
        }

        private void Number(Panel parent, string label, int value, Action<int> set)
        {
            var box = Field(parent, label, value.ToString(CultureInfo.InvariantCulture));
            readers.Add(() => {
                int parsed;
                if (!int.TryParse(box.Text, out parsed)) throw new ArgumentException(label + ": introduz um número inteiro.");
                set(parsed);
            });
        }

        private void Decimal(Panel parent, string label, double value, Action<double> set)
        {
            var box = Field(parent, label, value.ToString(CultureInfo.InvariantCulture));
            readers.Add(() => {
                double parsed;
                if (!double.TryParse(box.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    throw new ArgumentException(label + ": introduz um número.");
                set(parsed);
            });
        }

        private void Toggle(Panel parent, string label, bool value, Action<bool> set)
        {
            var box = new CheckBox { Content = label, IsChecked = value, Margin = new Thickness(8, 6, 8, 6) };
            parent.Children.Add(box);
            readers.Add(() => set(box.IsChecked == true));
        }

        private void BuildEditor()
        {
            readers.Clear(); Editor.Children.Clear();
            var connection = Section("1 · Ligação e disposição");
            var host = Field(connection, "Endereço IPv4 do WLED", draft.stripUrl);
            readers.Add(() => draft.stripUrl = host.Text.Trim());
            Number(connection, "Porta UDP (WLED)", draft.stripPort, x => draft.stripPort = x);
            Number(connection, "Quantidade de LEDs", draft.ledAmount, x => draft.ledAmount = x);
            Number(connection, "Primeiro LED (índice 0–255)", draft.offset, x => draft.offset = x);
            var direction = new ComboBox { Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 250 };
            direction.Items.Add("→ Início para o fim");
            direction.Items.Add("← Fim para o início");
            direction.Items.Add("← → Centro para as pontas");
            direction.Items.Add("→ ← Pontas para o centro");
            direction.SelectedIndex = draft.center ? (draft.mirror ? 3 : 2) : (draft.mirror ? 1 : 0);
            connection.Children.Add(direction);
            readers.Add(() => { draft.center = direction.SelectedIndex >= 2; draft.mirror = direction.SelectedIndex == 1 || direction.SelectedIndex == 3; });
            connection.Children.Add(new TextBlock { Text = "Configura no WLED a mesma porta UDP. O protocolo atual permite índices 0–255.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8), Opacity = 0.7 });

            var global = Section("2 · Brilho e animação");
            Number(global, "Brilho global (%)", draft.Brightness, x => draft.Brightness = x);
            Number(global, "Atualizações por segundo (1–60)", draft.FramesPerSecond, x => draft.FramesPerSecond = x);
            Number(global, "Duração bandeira verde (ms)", draft.GreenDurationMs, x => draft.GreenDurationMs = x);
            Number(global, "Duração dos testes (ms)", draft.TestDurationMs, x => draft.TestDurationMs = x);
            Decimal(global, "Início da barra RPM (%)", draft.RpmStartPercent, x => draft.RpmStartPercent = x);
            Decimal(global, "Barra RPM completa (%)", draft.RpmEndPercent, x => draft.RpmEndPercent = x);

            var effects = Section("3 · Efeitos");
            effects.Children.Add(new TextBlock {
                Text = "Maior prioridade prevalece. Os testes aplicam e guardam todas as definições do painel. Os sinais disponíveis dependem do jogo.",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8), Opacity = 0.7 });
            foreach (var effect in draft.Effects) {
                var panel = new StackPanel();
                var card = new Expander { Header = effect.Name, Content = panel, Margin = new Thickness(8, 4, 8, 4) };
                effects.Children.Add(card);
                Toggle(panel, "Ativar efeito", effect.Enabled, x => effect.Enabled = x);
                var colorRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8) };
                var swatch = new Border { Width = 32, Height = 28, Margin = new Thickness(0, 0, 8, 0) };
                var hex = new TextBox { Text = string.Format("#{0:X2}{1:X2}{2:X2}", effect.R, effect.G, effect.B), Width = 100 };
                var choose = new Button { Content = "Escolher cor…", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(8, 3, 8, 3) };
                Action preview = () => {
                    try { swatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex.Text)); }
                    catch (FormatException) { swatch.Background = Brushes.Transparent; }
                    catch (NotSupportedException) { swatch.Background = Brushes.Transparent; }
                };
                hex.TextChanged += (s, e) => preview(); preview();
                choose.Click += (s, e) => {
                    using (var dialog = new System.Windows.Forms.ColorDialog()) {
                        try { var c = (Color)ColorConverter.ConvertFromString(hex.Text); dialog.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B); }
                        catch (Exception ex) when (ex is FormatException || ex is NotSupportedException) { }
                        dialog.FullOpen = true;
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            hex.Text = string.Format("#{0:X2}{1:X2}{2:X2}", dialog.Color.R, dialog.Color.G, dialog.Color.B);
                    }
                };
                colorRow.Children.Add(swatch); colorRow.Children.Add(hex); colorRow.Children.Add(choose); panel.Children.Add(colorRow);
                readers.Add(() => {
                    if (hex.Text.Length != 7 || hex.Text[0] != '#') throw new ArgumentException(effect.Name + ": usa a cor #RRGGBB.");
                    int rgb;
                    if (!int.TryParse(hex.Text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                        throw new ArgumentException(effect.Name + ": cor inválida.");
                    effect.R = (rgb >> 16) & 255; effect.G = (rgb >> 8) & 255; effect.B = rgb & 255;
                });
                Number(panel, "Brilho do efeito (%)", effect.Brightness, x => effect.Brightness = x);
                Number(panel, "Prioridade (0–100)", effect.Priority, x => effect.Priority = x);
                Toggle(panel, "Piscar", effect.Blink, x => effect.Blink = x);
                Number(panel, "Tempo ligado / desligado (ms)", effect.IntervalMs, x => effect.IntervalMs = x);
                var test = new Button { Content = "Testar na fita", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(8), Padding = new Thickness(12, 6, 12, 6) };
                test.Click += (s, e) => Run(() => { ApplyDraft(); plugin.Test(effect.Key); Feedback.Text = "Teste iniciado; termina automaticamente."; });
                panel.Children.Add(test);
            }
        }

        private void ReadDraft()
        {
            foreach (var read in readers) read();
            draft.Validate();
        }

        private void ApplyDraft() { ReadDraft(); plugin.ApplySettings(Clone(draft)); }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is InvalidOperationException || ex is XmlException || ex is UnauthorizedAccessException)
            { Feedback.Text = "Não foi possível concluir: " + ex.Message; }
        }

        private void Apply(object sender, RoutedEventArgs e) => Run(() => { ApplyDraft(); Feedback.Text = "Definições aplicadas e guardadas."; });
        private void StopTest(object sender, RoutedEventArgs e) { plugin.StopTest(); Feedback.Text = "Teste terminado."; }

        private void Export(object sender, RoutedEventArgs e) => Run(() => {
            ReadDraft();
            var dialog = new SaveFileDialog { Filter = "Perfil WLED (*.wledprofile)|*.wledprofile", FileName = "o-meu-perfil.wledprofile" };
            if (dialog.ShowDialog() != true) return;
            var portable = Clone(draft);
            portable.stripUrl = new WLEDSettings().stripUrl;
            portable.stripPort = new WLEDSettings().stripPort;
            using (var stream = File.Create(dialog.FileName)) new XmlSerializer(typeof(WLEDSettings)).Serialize(stream, portable);
            Feedback.Text = "Perfil exportado sem o teu endereço IP. A importação preserva a ligação e a disposição locais.";
        });

        private void Import(object sender, RoutedEventArgs e) => Run(() => {
            var dialog = new OpenFileDialog { Filter = "Perfil WLED (*.wledprofile)|*.wledprofile" };
            if (dialog.ShowDialog() != true) return;
            WLEDSettings imported;
            var xmlSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1000000 };
            using (var reader = XmlReader.Create(dialog.FileName, xmlSettings))
                imported = (WLEDSettings)new XmlSerializer(typeof(WLEDSettings)).Deserialize(reader);
            if (imported.SchemaVersion > 2) throw new ArgumentException("Este perfil requer uma versão mais recente do plugin.");
            imported.stripUrl = draft.stripUrl; imported.stripPort = draft.stripPort;
            imported.ledAmount = draft.ledAmount; imported.offset = draft.offset;
            imported.mirror = draft.mirror; imported.center = draft.center;
            imported.Validate();
            draft = imported; BuildEditor();
            Feedback.Text = "Perfil carregado. Revê e clica Aplicar e guardar.";
        });

        private void Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Repor os efeitos e manter a ligação e a disposição?", "WLED", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            var defaults = new WLEDSettings();
            defaults.stripUrl = draft.stripUrl; defaults.stripPort = draft.stripPort;
            defaults.ledAmount = draft.ledAmount; defaults.offset = draft.offset;
            defaults.mirror = draft.mirror; defaults.center = draft.center;
            defaults.Migrate(); draft = defaults; BuildEditor();
            Feedback.Text = "Predefinições carregadas. Clica Aplicar e guardar.";
        }
    }
}
