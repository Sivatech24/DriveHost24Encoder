using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace DriveHost24Encoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FfmpegService encoder;
        private const string profilesFile = "profiles.json";

        public MainWindow()
        {
            InitializeComponent();

            encoder = new FfmpegService();
            encoder.OnLog += s => Dispatcher.Invoke(() => txtLog.AppendText(s));

            LoadProfiles();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtWidth.Text, out int w)) { AppendLog("Invalid width"); return; }
            if (!int.TryParse(txtHeight.Text, out int h)) { AppendLog("Invalid height"); return; }
            if (!int.TryParse(txtFPS.Text, out int fps)) { AppendLog("Invalid FPS"); return; }

            EncoderProfile profile = new EncoderProfile
            {
                Width = w,
                Height = h,
                FPS = fps,
                Bitrate = txtBitrate.Text,
                Profile = cmbProfile.Text
            };

            var metadata = new Metadata
            {
                Title = txtClipName.Text,
                Artist = txtArtist.Text,
                Director = txtDirector.Text,
                Producer = txtProducer.Text,
                Writer = txtWriter.Text,
                Year = txtYear.Text,
                Genre = txtGenre.Text,
                Publisher = txtPublisher.Text,
                ContentProvider = txtContentProvider.Text,
                EncodedBy = txtEncodedBy.Text,
                Author = txtAuthor.Text,
                Copyright = txtCopyright.Text,
                Comment = txtComment.Text
            };

            await encoder.StartEncoding(txtInput.Text, txtClipName.Text, profile, metadata);
        }

        private async void Resume_Click(object sender, RoutedEventArgs e)
        {
            // Resume simply starts encoding without re-splitting
            Start_Click(sender, e);
        }

        private void AppendLog(string text)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
        }

        private void LoadProfiles()
        {
            try
            {
                if (!File.Exists(profilesFile))
                {
                    // create default profile
                    var def = new EncoderProfile { Name = "Default", Width = 2160, Height = 3840, FPS = 60, Bitrate = "250M", Profile = "rext", Preset = "p7" };
                    var arr = new[] { def };
                    File.WriteAllText(profilesFile, Newtonsoft.Json.JsonConvert.SerializeObject(arr, Newtonsoft.Json.Formatting.Indented));
                }

                var json = File.ReadAllText(profilesFile);
                var profiles = Newtonsoft.Json.JsonConvert.DeserializeObject<EncoderProfile[]>(json);

                cmbProfile.Items.Clear();
                foreach (var p in profiles)
                {
                    cmbProfile.Items.Add(p.Name);
                }
                if (cmbProfile.Items.Count > 0) cmbProfile.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppendLog("Failed to load profiles: " + ex.Message);
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var list = new System.Collections.Generic.List<EncoderProfile>();
                if (File.Exists(profilesFile))
                {
                    list.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<EncoderProfile[]>(File.ReadAllText(profilesFile)));
                }

                var p = new EncoderProfile
                {
                    Name = txtProfileName.Text == string.Empty ? $"Profile_{list.Count + 1}" : txtProfileName.Text,
                    Width = int.TryParse(txtWidth.Text, out int w) ? w : 2160,
                    Height = int.TryParse(txtHeight.Text, out int h) ? h : 3840,
                    FPS = int.TryParse(txtFPS.Text, out int f) ? f : 60,
                    Bitrate = txtBitrate.Text,
                    Profile = cmbProfile.Text,
                    Preset = txtPreset.Text
                };

                list.Add(p);
                File.WriteAllText(profilesFile, Newtonsoft.Json.JsonConvert.SerializeObject(list.ToArray(), Newtonsoft.Json.Formatting.Indented));
                LoadProfiles();
                AppendLog("Profile saved.");
            }
            catch (Exception ex)
            {
                AppendLog("Failed to save profile: " + ex.Message);
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            // Basic edit: load selected profile into fields
            try
            {
                if (!File.Exists(profilesFile)) return;
                var profiles = Newtonsoft.Json.JsonConvert.DeserializeObject<EncoderProfile[]>(File.ReadAllText(profilesFile));
                var sel = cmbProfile.SelectedItem as string;
                var p = System.Array.Find(profiles, x => x.Name == sel);
                if (p == null) return;

                txtProfileName.Text = p.Name;
                txtWidth.Text = p.Width.ToString();
                txtHeight.Text = p.Height.ToString();
                txtFPS.Text = p.FPS.ToString();
                txtBitrate.Text = p.Bitrate;
                txtPreset.Text = p.Preset;
                AppendLog("Profile loaded for edit. Modify values and click Save Profile to persist.");
            }
            catch (Exception ex)
            {
                AppendLog("Failed to edit profile: " + ex.Message);
            }
        }
    }
}
