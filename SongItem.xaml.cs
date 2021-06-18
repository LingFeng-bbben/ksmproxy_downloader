using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
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

        public SongItem(song metaSong)
        {
            InitializeComponent();
            Data = metaSong;
        }

        public song Data
        {
            get { return _Data; }
            set
            {
                _Data = value;

                TitleBlock.Text = _Data.name;
                ArtistBlock.Text = _Data.artist;
            }
        }

        private song _Data;

        public async Task InitializeCard(song songData)
        {
            _Data = songData;
            Directory.CreateDirectory(MainWindow.imgfilePath);

            //请求图片
            var filepath = $"{MainWindow.imgfilePath}{_Data.id}";
            var request = $"{MainWindow.getpicAPI}?id={_Data.id}";
            if (!File.Exists(filepath)) await ksm_download.Download.downloadFile(request, filepath);

            var bmp = new BitmapImage();
            bmp.BeginInit(); //初始化
            bmp.UriSource = new Uri(filepath, UriKind.Absolute); //设置图片路径
            bmp.EndInit(); //结束初始化
            Cover.Source = bmp;
        }

        private void OverPreview(object sender, MouseEventArgs e)
        {
            Previewer.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1d, TimeSpan.FromSeconds(0.1)));
        }

        private void LeavePreview(object sender, MouseEventArgs e)
        {
            Previewer.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0d, TimeSpan.FromSeconds(0.1)));
        }

        private async void DoPreview(object sender, RoutedEventArgs e)
        {
            if (MainWindow.isPlaying)
            {
                MainWindow.mediaPlayer.Stop();

                var selfPlay = Icon.Kind != PackIconKind.Play;

                foreach (var item in MainWindow.SongsCollection)
                {
                    item.Icon.Kind = PackIconKind.Play;
                }

                MainWindow.isPlaying = false;

                if (selfPlay) return;
            }

            Icon.Kind = PackIconKind.Buffer;

            Directory.CreateDirectory(MainWindow.previewfilePath);
            
            string filepath = $"{MainWindow.previewfilePath}{_Data.id}_preview.mp3";
            string request = $"{MainWindow.getpreviewAPI}?id={_Data.id}";

            if (!File.Exists(filepath))
            {
                await ksm_download.Download.downloadFile(request, filepath);
            }

            MainWindow.mediaPlayer.Open(new Uri(filepath));
            MainWindow.mediaPlayer.Play();
            MainWindow.isPlaying = true;
            Icon.Kind = PackIconKind.Stop;
            MainWindow.mediaPlayer.MediaEnded += (o, args) =>
            {
                Icon.Kind = PackIconKind.Play;
                MainWindow.isPlaying = false;
            };
        }

        private async void DoDownload(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "Cookie", MainWindow.cookie } };

            if (await Download.downloadText(MainWindow.cachestaAPI + _Data.id) == "0")
            {
                if (MainWindow.cookie != "")
                {
                    SongListJsonRoot jsroot = await Download.downloadJson<SongListJsonRoot>(MainWindow.reqDownAPI + _Data.id, keyValuePairs);
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
                    MessageBox.Show("下载" + _Data.name + "出现问题\n本歌曲还未缓存，请登录请求缓存", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                    RequestLogin();
                    return;
                }
            }
            string downloadFilename = MainWindow.songDownloadPath + _Data.id + ".zip";

            try
            {
                WebClient wc = new WebClient();
                DownloadProgress.IsIndeterminate = true;
                wc.DownloadProgressChanged += (o, args) =>
                {
                    DownloadProgress.IsIndeterminate = false;
                    DownloadProgress.Maximum = args.TotalBytesToReceive;
                    DownloadProgress.Value = args.BytesReceived;
                };

                wc.DownloadFileCompleted += (o, args) =>
                {
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    ButtonDownload.IsEnabled = false;
                };
                await wc.DownloadFileTaskAsync(MainWindow.songDownloadUrl + _Data.id, downloadFilename);

                var extFilepath = MainWindow.songExtPath + _Data.id + "/";

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
            catch (Exception ex)
            {
                MessageBox.Show($"下载 \"{_Data.name}\" 出现问题:" + ex.Message);
                return;
            }
        }

        public delegate void OnRequestLogin();

        public event OnRequestLogin RequestLogin;

    }
}