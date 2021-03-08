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
                OnPropertyChanged("id");
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
        const string songlistAPI = "https://ksm-proxy.littlec.tunergames.com/api/songs";
        const string getpicAPI = "https://ksm-proxy.littlec.tunergames.com/api/getJacket";
        const string getpreviewAPI = "https://ksm-proxy.littlec.tunergames.com/api/getPreview";
        const string loginAPI = "https://ksm-proxy.littlec.tunergames.com/api/login";
        const string usrinfoAPI = "https://ksm-proxy.littlec.tunergames.com/api/userInfo";
        const string reqDownAPI = "https://ksm-proxy.littlec.tunergames.com/api/applyDownload?id=";
        const string songDownloadUrl = "https://ksm-proxy.littlec.tunergames.com/download/";
        SongData sd = new SongData();//现在显示的歌曲列表(与显示列表绑定)
        SongListJson jslist = new SongListJson();//下载的列表
        int currentPage = 1;
        public MainWindow()
        {
            InitializeComponent();
        }

        void printLog(string a)
        {
            log.Text = a + "\n"+ log.Text;
        }

        delegate void ProgressChange(int a);

        void SetProgressValue(int a) => progressBar.Value = a;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            printLog("ksm下载器v0.4k");
            if (File.Exists(cookiefilePath))
            {
                cookie = File.ReadAllText(cookiefilePath);
                if (!await updateUsrInfo())
                {
                    panel_login.Visibility = Visibility.Visible;
                }
            }
            else
            {
                panel_login.Visibility = Visibility.Visible;
            }
            Refresh_list();
        }

        
        private async void Refresh_list()
        {
            printLog(String.Format("正在获取歌曲列表,第{0}页",currentPage));
            sd = new SongData();//清空列表
            button_next.IsEnabled = false;
            button_prev.IsEnabled = false;
            Page_Box.IsEnabled = false;
            Directory.CreateDirectory(Environment.CurrentDirectory + "/ksmdownload");
            var jsroot = await Download.downloadJson<SongListJsonRoot>(songlistAPI+"?page="+currentPage);
            jslist = jsroot.Data;

            foreach(var a in jslist.Data)
            {
                string[] levels = new string[4];
                string charters = "";
                foreach(var chart in a.Charts)
                {
                    levels[chart.Difficulty-1] = chart.Level.ToString();
                    if (chart.Effector != charters && charters != "") charters += " / " + chart.Effector;
                    else if (chart.Effector != charters) charters = chart.Effector;
                }
                song thissong = new song(a.Id.ToString(), a.Title, levels, a.Artist,charters);
                sd.songslist.Add(thissong);
            }
            
            songlistview.ItemsSource = sd.songslist;
            printLog("获取歌曲列表完成");
            button_next.IsEnabled = true;
            button_prev.IsEnabled = true;
            Page_Box.IsEnabled = true;
        }

        static string songExtPath = Environment.CurrentDirectory + "/songs/ksm-download/";
        static string songDownloadPath = Environment.CurrentDirectory + "/songs/";
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(songExtPath);
            progressBar.IsIndeterminate = true;
            await DownloadSelected();
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
                printLog("正在下载第" + (i + 1) + "个，共" + selected.Count + "个");
                printLog(selected[i].name);
                printLog("请求服务器缓存...");
                SongListJsonRoot jsroot = await Download.downloadJson<SongListJsonRoot>(reqDownAPI + selected[i].id.ToString(), keyValuePairs);
                if (jsroot.Code == 404)
                {
                    printLog("歌曲不存在");
                    continue;
                }
                if (jsroot.Code == 403)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                    MessageBox.Show("本日下载次数已用光", "【悲报】", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string downloadFilename = songDownloadPath + selected[i].id + ".zip";
                printLog("正在下载");
                if (!await Download.downloadFile(songDownloadUrl + selected[i].id.ToString(), downloadFilename )) {
                    printLog("下载失败");
                    continue;
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
                }
                printLog("解压完毕");
                File.Delete(downloadFilename);
            }
            System.Media.SystemSounds.Beep.Play();


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            log.Text = "";
            //清空log
        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("tencent://AddContact/?fromId=45&fromSubId=1&subcmd=all&uin=1323291094");
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
                Directory.CreateDirectory(imgfilePath);
                var thesong = jslist.Data.First(o => o.Id == selected.id);
                //请求图片
                string filepath = String.Format("{0}{1}_{2}", imgfilePath, thesong.Id.ToString(), thesong.JacketFilename);
                string request = String.Format("{0}?id={1}", getpicAPI, thesong.Id.ToString());
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
            }

        }


        private void button1_Copy_Click(object sender, RoutedEventArgs e)
        {
            Refresh_list();
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


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Search")
            {
                SearchBox.Text = "";
            }
            
        }

        private void button_next_Click(object sender, RoutedEventArgs e)
        {
            int page = currentPage;
            page++;
            Page_Box.Text = page.ToString();
        }

        private void button_prev_Click(object sender, RoutedEventArgs e)
        {
            int page = currentPage;
            if (page < 2) page = 1;
            else page--;
            Page_Box.Text = page.ToString();
        }

        private void Page_Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            int page = 1;
            try
            {
                page = int.Parse(Page_Box.Text);
                if (page != currentPage)
                {
                    if (page < 2) currentPage = 1;
                    else currentPage = page;

                    Refresh_list();
                }
            }
            catch
            {
                Page_Box.Text = "1";
                if (currentPage != 1)
                {
                    currentPage = 1;
                    Refresh_list();
                }
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
                var thesong = jslist.Data.First(o => o.Id == selected.id);
                //请求图片
                string filepath = String.Format("{0}{1}_{2}", previewfilePath, thesong.Id.ToString(), "preview.mp3");
                string request = String.Format("{0}?id={1}", getpreviewAPI, thesong.Id.ToString());
                
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
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string> { { "Cookie", cookie } };
            UserJsonRoot jsonRoot = await Download.downloadJson<UserJsonRoot>(usrinfoAPI, keyValuePairs);
            if (jsonRoot.Code != 200)
            {
                printLog(jsonRoot.Msg);
                return false;
            }
            label_user.Content = String.Format("剩余下载次数:{0}    {1}", jsonRoot.Data.AvailableDownloadTimes, jsonRoot.Data.Usrname);
            return true;
        }

        private void label_user_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
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
            System.Diagnostics.Process.Start("http://netedu.xauat.edu.cn/jpkc/netedu/jpkc/gdsx/homepage/5jxsd/51/513/5308/530805.htm");
        }
    }
}
