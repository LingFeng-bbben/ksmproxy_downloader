using System;
using System.Collections.Generic;
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

namespace ksm_download
{
    /// <summary>
    /// 和listview绑定的类
    /// </summary>
    public class song
    {
        public Guid id { get; set; }
        public string downState { get; set; }
        public string name { get; set; }
        public string[] levels { get; set; }
        public string artist { get; set; }
        public string charter { get; set; }
        public song(string _id, string _name, string[] _lv, string _art,string _charter)
        {
            id = new Guid(_id);
            name = _name;
            artist = _art;
            levels = _lv;
            charter = _charter;
        }
    }

    public class SongData:INotifyPropertyChanged
    {
        List<song> _songslist = new List<song>();
        public List<song> songslist { 
            get 
            {
                return _songslist;
            } 
            set
            {
                OnPropertyChanged("已下载");
                _songslist = songslist;
            }
                                        
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public partial class MainWindow : Window
    {
        const string songlistAPI = "https://ksm-api.littlec.xyz:10443/api/songs";
        const string getpicAPI = "https://ksm-api.littlec.xyz:10443/api/getJacket";
        const string getpreviewAPI = "https://ksm-api.littlec.xyz:10443/api/getPreview";
        const string loginAPI = "https://ksm-api.littlec.xyz:10443/api/login";
        const string usrinfoAPI = "https://ksm-api.littlec.xyz:10443/api/userInfo";
        const string reqDownAPI = "https://ksm-api.littlec.xyz:10443/api/applyDownload?id=";
        const string cachestaAPI = "https://ksm-api.littlec.xyz:10443/api/getCacheStatus?id=";
        const string songDownloadUrl = "https://ksm-api.littlec.xyz:10443/download/";
        SongData sd = new SongData();//现在显示的歌曲列表(与显示列表绑定)
        SongListJson jslist = new SongListJson();//下载的列表
        int maxPage = 555;
        public MainWindow()
        {
            InitializeComponent();
        }

        void printLog(string a)
        {
            log.Text = a + "\n"+ log.Text;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            printLog("ksm下载器v0.4k");
            Directory.CreateDirectory(Environment.CurrentDirectory + "/ksmdownload");
            Directory.CreateDirectory(songExtPath);

            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            if (File.Exists(cookiefilePath))
            {
                cookie = File.ReadAllText(cookiefilePath);
                if (!await updateUsrInfo())
                {
                    panel_login.Visibility = Visibility.Visible;
                }
                else
                {
                    panel_login.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                panel_login.Visibility = Visibility.Visible;
            }
            Refresh_list();
        }

        int currentPage = 1;
        private async void Refresh_list(int page=1,bool clear = false)
        {
            printLog(String.Format("正在获取歌曲列表,第{0}页",page));
            var sdnew = new SongData();//清空列表
            if (!clear) sdnew.songslist.AddRange(sd.songslist);

            SongListJsonRoot jsroot = new SongListJsonRoot();
            if (SearchBox.Text != "Search" && SearchBox.Text != "")
            {
                jsroot = await Download.downloadJson<SongListJsonRoot>(songlistAPI +"?songs="+SearchBox.Text+ "&page=" + page);
            }
            else {
                jsroot = await Download.downloadJson<SongListJsonRoot>(songlistAPI + "?page=" + page);
            }
            

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
                    if(await Download.downloadText(cachestaAPI + a.Id) == "1")
                    {
                        thissong.downState = "○";//已缓存
                    }
                    sdnew.songslist.Add(thissong);
                }
            }

            maxPage = jslist.Meta.LastPage;

            if (sdnew.songslist.Exists(o => o.name == "LOAD MORE"))
            {
                sdnew.songslist.Remove(sdnew.songslist.Find(o => o.name == "LOAD MORE"));
            }
            if (currentPage + 1 < maxPage)
            {
                song dummy = new song(Guid.Empty.ToString(), "LOAD MORE", new string[4] { "", "", "", "" }, "", "");
                sdnew.songslist.Add(dummy);
            }

            
            sd = sdnew;
            songlistview.ItemsSource = sd.songslist;
            CheckDownloaded();
            printLog("获取歌曲列表完成");
        }

        static string songExtPath = Environment.CurrentDirectory + "/songs/ksm-download/";
        static string songDownloadPath = Environment.CurrentDirectory + "/songs/";
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(songExtPath);
            progressBar.IsIndeterminate = true;
            await DownloadSelected();
            System.Media.SystemSounds.Beep.Play();
            CheckDownloaded();
            progressBar.IsIndeterminate = false;
            await updateUsrInfo();
        }

        private async Task DownloadSelected()
        {
            System.Collections.IList se = songlistview.SelectedItems;
            var collection = se.Cast<song>();
            List<song> selected = collection.ToList(); 

            Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "Cookie", cookie } };
            for (int i = 0; i < selected.Count; i++)
            {
                if (await Download.downloadText(cachestaAPI + selected[i].id.ToString()) == "0")
                {
                    if (cookie != "")
                    {
                        printLog("请求服务器缓存...");
                        SongListJsonRoot jsroot = await Download.downloadJson<SongListJsonRoot>(reqDownAPI + selected[i].id.ToString(), keyValuePairs);
                        if (jsroot.Code == 404)
                        {
                            MessageBox.Show("歌曲不存在", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
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
                        MessageBox.Show("下载"+ selected[i].name+"出现问题\n本歌曲还未缓存，请登录请求缓存", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                        panel_login.Visibility = Visibility.Visible;
                        return;
                    }
                }
                string downloadFilename = songDownloadPath + selected[i].id + ".zip";
                printLog(String.Format("正在下载({0}/{1}):{2}",(i + 1),selected.Count, selected[i].name));
                if (!await Download.downloadFile(songDownloadUrl + selected[i].id.ToString(), downloadFilename )) {
                    printLog("下载失败");
                    return;
                };
                printLog("下载完毕");
                string extFilepath = songExtPath + selected[i].id.ToString() + "/";
                try
                {
                    if (Directory.Exists(extFilepath))
                        Directory.Delete(extFilepath, true);
                    ZipFile.ExtractToDirectory(downloadFilename, extFilepath);
                }
                catch (Exception ex)
                {
                    printLog("解压出现问题:" + ex.Message);
                    return;
                }
                printLog("解压完毕");
                File.Delete(downloadFilename);
            }
        }

        private void CheckDownloaded()
        {
            DirectoryInfo dirinfo = new DirectoryInfo(songExtPath);
            List<string> ids = new List<string>();
            foreach(var a in dirinfo.GetDirectories())
            {
                ids.Add(a.Name);
            }
            SongData newList = new SongData();
            newList.songslist.AddRange(sd.songslist);
            foreach(var a in newList.songslist)
            {
                if (ids.Exists(o => o == a.id.ToString())){
                    a.downState = "◉";
                }
            }
            sd = newList;
            songlistview.ItemsSource = sd.songslist;
        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("tencent://groupwpa/?subcmd=all\x26param=7b2267726f757055696e223a3437313236333538372c2274696d655374616d70223a313631353139313937392c22617574684b6579223a224e78615a4d48774e3171393852397a56564f59675448336b706368524b55715a6b396f38773378667076616241426a515a333159326772575763654367666273222c2261757468223a22227d");
            //打开qq窗口
        }



        static string imgfilePath = Environment.CurrentDirectory + "/ksmdownload/imgtmp/";
        /// <summary>
        /// 载入图片
        /// </summary>
        private async void songlistview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mediaPlayer.Stop();
            isPlaying = false;
            button_preview.Content = "▶";

            song selected = (song)songlistview.SelectedItem;

            if (selected != null)
            {
                if (selected.name == "LOAD MORE")
                {
                    currentPage++;
                    Refresh_list(currentPage);
                    return;
                }

                Directory.CreateDirectory(imgfilePath);
                //请求图片
                string filepath = String.Format("{0}{1}", imgfilePath, selected.id);
                string request = String.Format("{0}?id={1}", getpicAPI, selected.id);
                if (!File.Exists(filepath))
                {
                    SongImg.Opacity = 0.5;
                    progressBar.IsIndeterminate = true;
                    printLog("正在载图");
                    await Download.downloadFile(request, filepath);
                    progressBar.IsIndeterminate = false;
                    SongImg.Opacity = 1;
                }

                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();//初始化
                bmp.UriSource = new Uri(filepath, UriKind.Absolute);//设置图片路径
                bmp.EndInit();//结束初始化
                SongImg.Source = bmp;
                printLog("");
            }

        }

        
        private void button1_Copy_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            Refresh_list(1,true);
        }

        //排序
        private ListSortDirection _sortDirection;
        private GridViewColumnHeader _sortColumn;
        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = e.OriginalSource as GridViewColumnHeader;
            if (column == null || column.Column == null)
            {
                return;
            }

            if (_sortColumn == column)
            {
                // Toggle sorting direction 
                _sortDirection = _sortDirection == ListSortDirection.Ascending ?
                                                   ListSortDirection.Descending :
                                                   ListSortDirection.Ascending;
            }
            else
            {
                // Remove arrow from previously sorted header 
                if (_sortColumn != null && _sortColumn.Column != null)
                {
                    _sortColumn.Column.HeaderTemplate = null;

                }

                _sortColumn = column;
                _sortDirection = ListSortDirection.Ascending;

            }

            if (_sortDirection == ListSortDirection.Ascending)
            {
                column.Column.HeaderTemplate = Resources["ArrowUp"] as DataTemplate;
            }
            else
            {
                column.Column.HeaderTemplate = Resources["ArrowDown"] as DataTemplate;
            }

            string header = string.Empty;

            // if binding is used and property name doesn't match header content 
            Binding b = _sortColumn.Column.DisplayMemberBinding as Binding;
            if (b != null)
            {
                header = b.Path.Path;
            }

            ICollectionView resultDataView = CollectionViewSource.GetDefaultView(
                                                       (sender as ListView).ItemsSource);
            resultDataView.SortDescriptions.Clear();
            resultDataView.SortDescriptions.Add(
                                        new SortDescription(header, _sortDirection));

        }

        //搜索
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            timer.Stop();
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (SearchBox.Text != "Search")
                {
                    currentPage = 1;
                    Refresh_list(1,true);
                }
            }));
            
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "")
            {
                SearchBox.Text = "Search";
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Search")
            {
                SearchBox.Text = "";
            }
            
        }

        static string previewfilePath = Environment.CurrentDirectory + "/ksmdownload/prevtmp/";
        MediaPlayer mediaPlayer = new MediaPlayer();
        bool isPlaying = false;
        /// <summary>
        ///    载入试听
        /// </summary>
        private async void button_preview_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaPlayer.Stop();
                button_preview.Content = "▶";
                isPlaying = false;
                return;
            }
            song selected = (song)songlistview.SelectedItem;
            if (selected != null)
            {
                Directory.CreateDirectory(previewfilePath);
                //请求图片
                string filepath = String.Format("{0}{1}_{2}", previewfilePath, selected.id, "preview.mp3");
                string request = String.Format("{0}?id={1}", getpreviewAPI, selected.id);
                
                button_preview.IsEnabled = false;
                progressBar.IsIndeterminate = true;
                button_preview.Content = "...";
                
                if (!File.Exists(filepath))
                {
                    printLog("正在下载试听");
                    await Download.downloadFile(request, filepath);
                }

                button_preview.Content = "■";
                progressBar.IsIndeterminate = false;
                button_preview.IsEnabled = true;

                mediaPlayer.Open(new Uri(filepath));
                mediaPlayer.Play();
                isPlaying = true;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                printLog("");
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            button_preview.Content = "▶";
            isPlaying = false;
        }

        static string cookiefilePath = Environment.CurrentDirectory + "/ksmdownload/cookie";
        string cookie = "";
        private async void button_login_Click(object sender, RoutedEventArgs e)
        {
            string usrname = textbox_usrname.Text;
            string password = textbox_password.Password;
            if (usrname == "" || password == "")
            {
                MessageBox.Show("傻逼", "傻逼", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string sendback = await Download.login(loginAPI, usrname, password);
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
                printLog("获取用户信息失败:"+jsonRoot.Msg);
                return false;
            }
            if (jsonRoot.Data.AvailableDownloadTimes == 2147483646)
            {
                label_user.Content = "𝖁𝕴𝕻  " + jsonRoot.Data.Usrname;
                return true;
            }
            else
            {
                label_user.Content = String.Format("剩余下载次数:{0}    {1}", jsonRoot.Data.AvailableDownloadTimes, jsonRoot.Data.Usrname);
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
