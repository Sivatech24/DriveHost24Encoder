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

namespace DriveHost24Encoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private FfmpegService encoder = new FfmpegService();

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            EncoderProfile profile = new EncoderProfile
            {
                Width = int.Parse(txtWidth.Text),
                Height = int.Parse(txtHeight.Text),
                FPS = int.Parse(txtFPS.Text),
                Bitrate = txtBitrate.Text
            };

            await encoder.StartEncoding(txtInput.Text, txtClipName.Text, profile);
        }

        private async void Resume_Click(object sender, RoutedEventArgs e)
        {
            await Start_Click(sender, e);
        }
    }
}
