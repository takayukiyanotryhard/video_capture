using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Threading;
using System.Windows;

namespace capture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        bool recording = false;
        object lock_obj = new();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_rec_Click(object sender, RoutedEventArgs e)
        {
            lock (lock_obj)
            {
                if (recording) return;
                recording = true;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                panel.Background = System.Windows.Media.Brushes.LightPink;
            });

            Thread thread = new Thread(new ThreadStart(() =>
            {
                CaptureMovieAsync();
            }));
            thread.Start();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            lock (lock_obj)
            {
                recording = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    panel.Background = System.Windows.Media.Brushes.White;
                });
            }
        }
        public static string GetCurrentAppDir()
        {
            var path = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            return path ?? "";
        }
        private void btn_open_folder_Click(object sender, RoutedEventArgs e)
        {
            var path = GetCurrentAppDir();
            if (System.IO.Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
        }
        private string CurrentTimeStr()
        {
            return string.Format("yyyyMMdd_HHmmss.fff", DateTime.Now);
        }
        private void CaptureMovieAsync()
        {
            string path = "rec_" + CurrentTimeStr() + ".wmv";
            using var writer = new VideoWriter(path, FourCC.WMV3, 5, new OpenCvSharp.Size((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight));
            bool status = true;
            while (status)
            {
                using (var screenBmp = new Bitmap(
                    (int)SystemParameters.PrimaryScreenWidth,
                    (int)SystemParameters.PrimaryScreenHeight,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using var bmpGraphics = Graphics.FromImage(screenBmp);
                    bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenBmp.Size);

                    Dispatcher.Invoke(() =>
                    {
                        Mat mat = BitmapConverter.ToMat(screenBmp).CvtColor(ColorConversionCodes.RGB2BGR);
                        Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2RGB);
                        Cv2.Resize(mat, mat, new OpenCvSharp.Size((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight));
                        writer.Write(mat);
                    });
                }
                Thread.Sleep(100);
                lock (lock_obj)
                {
                    status = recording;
                }
            }
        }
    }
}
