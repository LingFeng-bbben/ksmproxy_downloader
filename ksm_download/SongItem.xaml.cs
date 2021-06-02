using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ksm_download.JsonFormats;
using MaterialDesignThemes.Wpf;

namespace ksm_download
{
    /// <summary>
    ///     Interaction logic for SongItem.xaml
    /// </summary>
    public partial class SongItem : UserControl
    {
        public SongItem()
        {
            InitializeComponent();
        }

        public song Data;

        private async void Card_Initialized(object sender, EventArgs e)
        {
            Directory.CreateDirectory(MainWindow.imgfilePath);

            //请求图片
            var filepath = $"{MainWindow.imgfilePath}{Data.id}";
            var request = $"{MainWindow.getpicAPI}?id={Data.id}";
            if (!File.Exists(filepath)) await ksm_download.Download.downloadFile(request, filepath);

            var bmp = new BitmapImage();
            bmp.BeginInit(); //初始化
            bmp.UriSource = new Uri(filepath, UriKind.Absolute); //设置图片路径
            bmp.EndInit(); //结束初始化
            Cover.Source = bmp;
        }

        private void OverPreview(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var da = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(0.3))
            };
            Previewer.BeginAnimation(Border.OpacityProperty, da);
        }

        private async void DoPreview(object sender, RoutedEventArgs e)
        {
            if (MainWindow.isPlaying)
            {
                MainWindow.mediaPlayer.Stop();

                if (Icon.Kind == PackIconKind.Play)
                {
                    Icon.Kind = PackIconKind.Stop;
                }
                else
                {
                    Icon.Kind = PackIconKind.Play;
                }
                
                MainWindow.isPlaying = false;
                return;
            }

            Directory.CreateDirectory(MainWindow.previewfilePath);
            //请求图片
            string filepath = $"{MainWindow.previewfilePath}{Data.id}_{"preview.mp3"}";
            string request = $"{MainWindow.getpreviewAPI}?id={Data.id}";

            if (!File.Exists(filepath))
            {
                await ksm_download.Download.downloadFile(request, filepath);
            }

            MainWindow.mediaPlayer.Open(new Uri(filepath));
            MainWindow.mediaPlayer.Play();
            MainWindow.isPlaying = true;
            MainWindow.mediaPlayer.MediaEnded += (o, args) =>
            {
                Icon.Kind = PackIconKind.Play;
                MainWindow.isPlaying = false;
            };
        }

        private async void DoDownload(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "Cookie", MainWindow.cookie } };

            if (await Download.downloadText(MainWindow.cachestaAPI + Data.id) == "0")
            {
                if (MainWindow.cookie != "")
                {
                    SongListJsonRoot jsroot = await Download.downloadJson<SongListJsonRoot>(MainWindow.reqDownAPI + Data.id, keyValuePairs);
                    if (jsroot.Code == 404)
                    {
                        MessageBox.Show("歌曲不存在", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    if (jsroot.Code == 403)
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                        MessageBox.Show("本日请求次数已用光", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("下载" + Data.name + "出现问题\n本歌曲还未缓存，请登录请求缓存", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                    RequestLogin();
                    return;
                }
            }
            string downloadFilename = MainWindow.songDownloadPath + Data.id + ".zip";
            if (!await Download.downloadFile(MainWindow.songDownloadUrl + Data.id, downloadFilename))
            {
                return;
            };
            string extFilepath = MainWindow.songExtPath + Data.id.ToString() + "/";
            try
            {
                if (Directory.Exists(extFilepath))
                    Directory.Delete(extFilepath, true);
                ZipFile.ExtractToDirectory(downloadFilename, extFilepath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("解压出现问题:" + ex.Message);
                return;
            }
            File.Delete(downloadFilename);
        }

        public delegate void OnRequestLogin();

        public event OnRequestLogin RequestLogin;

        public delegate void OnDownloadedChart();

        public event OnDownloadedChart ChartDownloaded;
    }
}