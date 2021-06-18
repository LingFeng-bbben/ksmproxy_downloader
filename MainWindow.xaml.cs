using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO.Compression;
using System.ComponentModel;
using System.Windows.Data;
using System.Linq;
using ksm_download.JsonFormats;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Diagnostics;
using MaterialDesignThemes.Wpf;

namespace ksm_download
{

    public partial class MainWindow : Window
    {
        public const string updateAPI = "https://ksm-fanmade.oss-cn-shanghai.aliyuncs.com/update.json";
        public const string updateFile = "https://ksm-fanmade.oss-cn-shanghai.aliyuncs.com/ksmproxy-downloader.zip";
        public const string songlistAPI = "https://ksm-api.littlec.xyz:10443/api/songs";
        public const string getpicAPI = "https://ksm-api.littlec.xyz:10443/api/getJacket";
        public const string getpreviewAPI = "https://ksm-api.littlec.xyz:10443/api/getPreview";
        public const string loginAPI = "https://ksm-api.littlec.xyz:10443/api/login";
        public const string usrinfoAPI = "https://ksm-api.littlec.xyz:10443/api/userInfo";
        public const string reqDownAPI = "https://ksm-api.littlec.xyz:10443/api/applyDownload?id=";
        public const string cachestaAPI = "https://ksm-api.littlec.xyz:10443/api/getCacheStatus?id=";
        public const string songDownloadUrl = "https://ksm-api.littlec.xyz:10443/download/";

        public static string ksmPath = Environment.CurrentDirectory + "/ksmdownload";
        public static string songExtPath = Environment.CurrentDirectory + "/songs/ksm-download/";
        public static string songDownloadPath = Environment.CurrentDirectory + "/songs/";
        public static string imgfilePath = Environment.CurrentDirectory + "/ksmdownload/imgtmp/";
        public static string previewfilePath = Environment.CurrentDirectory + "/ksmdownload/prevtmp/";
        public static string cookiefilePath = Environment.CurrentDirectory + "/ksmdownload/cookie";

        const float currentVersion = 0.5f;

        public static string cookie = "";
        public static int currentPage = 1;
        public static int maxPage = 555;
        public static bool isPlaying = false;

        public static ObservableCollection<SongItem> SongsCollection = new ObservableCollection<SongItem>();//现在显示的歌曲列表(与显示列表绑定)
        public SongListJson jslist = new SongListJson();//下载的列表

        public static MediaPlayer mediaPlayer = new MediaPlayer();

        public MainWindow()
        {
            InitializeComponent();
            SongItems.ItemsSource = SongsCollection;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckUpdate();

            Directory.CreateDirectory(ksmPath);
            Directory.CreateDirectory(songExtPath);

            if (File.Exists(cookiefilePath))
            {
                cookie = File.ReadAllText(cookiefilePath);
                panel_login.Visibility = !await updateUsrInfo() ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                panel_login.Visibility = Visibility.Visible;
            }
            GetPage();
        }

        private async Task CheckUpdate()
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.CurrentDirectory);
            foreach(var a in dir.GetFiles())
            {
                if (a.Name.EndsWith(".old"))
                    a.Delete();
            }

            UpdateJson json = await Download.downloadJson<UpdateJson>(updateAPI);
            if (json.Version > currentVersion)
            {
                MessageBox.Show("即将更新到版本" + json.Version + ":\n" + json.Info, "更新");
                string zippath = ksmPath + "/new.zip";
                await Download.downloadFile(updateFile, zippath);

                ZipArchive arc = ZipFile.OpenRead(zippath);
                foreach(var a in arc.Entries)
                {
                    if (File.Exists(a.Name))
                        File.Move(a.Name, a.Name + ".old");
                }
                ZipFile.ExtractToDirectory(zippath, Environment.CurrentDirectory);
                Process.Start("ksm_download.exe");
                Application.Current.Shutdown();
            }
        }

        private async void GetPage(int page = 1)
        {
            try
            {
                var sdnew = new SongData();

                SongListJsonRoot jsroot = new SongListJsonRoot();

                jsroot = await Download.downloadJson<SongListJsonRoot>(songlistAPI + "?page=" + page);
                //jsroot = await Download.downloadJson<SongListJsonRoot>(songlistAPI + "?songs=" + SearchBox.Text + "&page=" + page);


                if (jsroot.Data.Data != null)
                {
                    jslist = jsroot.Data;
                    foreach (var a in jslist.Data)
                    {
                        string[] levels = new string[4];
                        string charters = "";
                        foreach (var chart in a.Charts)
                        {
                            levels[chart.Difficulty - 1] = chart.Level.ToString();
                            if (chart.Effector != charters && charters != "") charters += " / " + chart.Effector;
                            else if (chart.Effector != charters) charters = chart.Effector;
                        }
                        song thissong = new song(a.Id.ToString(), a.Title, levels, a.Artist, charters);
                        if (await Download.downloadText(cachestaAPI + a.Id) == "1")
                        {
                            thissong.downState = "○";//已缓存
                        }
                        sdnew.songslist.Add(thissong);
                    }
                }

                maxPage = jslist.Meta.LastPage;

                RefreshList(sdnew.songslist);

                Title = $"kp_d - {SongsCollection.Count} loaded";
            }
            catch (Exception)
            {
                //ignored
            }
        }

        public void RefreshList(List<song> songs)
        {
            SongsCollection.Clear();

            songs.ForEach(s =>
            {
                var thisCard = new SongItem(s);
                thisCard.RequestLogin += PromptLogin;
                SongsCollection.Add(thisCard);
            });
        }

        private void PromptLogin()
        {
            panel_login.Visibility = Visibility.Visible;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("tencent://groupwpa/?subcmd=all\x26param=7b2267726f757055696e223a3437313236333538372c2274696d655374616d70223a313631353139313937392c22617574684b6579223a224e78615a4d48774e3171393852397a56564f59675448336b706368524b55715a6b396f38773378667076616241426a515a333159326772575763654367666273222c2261757468223a22227d");
            //打开qq窗口
        }

        private async void button_login_Click(object sender, RoutedEventArgs e)
        {
            var username = textbox_usrname.Text;
            var password = textbox_password.Password;
            if (username == "" || password == "")
            {
                MessageBox.Show("请把用户名和密码框填写完毕哦qwq", "信息不足", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string sendback = await Download.login(loginAPI, username, password);
            if (sendback == "403")
            {
                MessageBox.Show("账号或密码错误", "提示", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            cookie = sendback.Substring(0, 75);
            File.WriteAllText(cookiefilePath, cookie);
            panel_login.Visibility = Visibility.Collapsed;
            await updateUsrInfo();
            return;
        }

        async Task<bool> updateUsrInfo()
        {
            if (cookie == "") return false;
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "Cookie", cookie } };
            UserJsonRoot jsonRoot = await Download.downloadJson<UserJsonRoot>(usrinfoAPI, keyValuePairs);
            if (jsonRoot.Code != 200)
            {
                MessageBox.Show("获取用户信息失败:"+jsonRoot.Msg);
                return false;
            }
            if (jsonRoot.Data.AvailableDownloadTimes == 2147483646)
            {
                label_user.Content = "𝖁𝕴𝕻  " + jsonRoot.Data.Username;
                return true;
            }
            else
            {
                label_user.Content = String.Format("剩余下载次数:{0}    {1}", jsonRoot.Data.AvailableDownloadTimes, jsonRoot.Data.Username);
                return true;
            }
        }

        private void label_user_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(cookie == "")
            {
                panel_login.Visibility = Visibility.Visible;
                return;
            }
            if(MessageBox.Show("要退出登录吗", "提示", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
            {
                File.Delete(cookiefilePath);
                cookie = "";
                label_user.Content = "GUEST";
                panel_login.Visibility = Visibility.Visible;
            }
        }

        private void label_register_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ksm.littlec.xyz/register");
        }

        private void label_skip_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            cookie = "";
            label_user.Content = "GUEST";
            panel_login.Visibility = Visibility.Collapsed;
        }

        private void button_Money_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://afdian.net/@little_c");
        }
    }
}
