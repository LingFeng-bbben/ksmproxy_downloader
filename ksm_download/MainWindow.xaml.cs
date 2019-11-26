using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;
using Aliyun.OSS;
using System.IO.Compression;
using System.ComponentModel;
using System.Windows.Data;

namespace ksm_download
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public class song
    {
        public string id { get; set; }
        public string name { get; set; }
        public string level { get; set; }
        public string[] levels { get; set; }
        public string artist { get; set; }
        public song(string _id, string _name, string _lv, string _art)
        {
            id = _id;
            name = _name;
            level = _lv;
            artist = _art;
            levels = level.Split('/');
        }
    }
    public partial class MainWindow : Window
    {

        string endpoint = "oss-cn-shanghai.aliyuncs.com";
        string accessKeyId = "LTAIGTuicNd5bdm3";
        string accessKeySecret = "hxsgrUuol43h2vwBIoyckb3Bhz91Dv";
        string bucketName = "ksm-songs";
        OssClient client;

        public MainWindow()
        {
            InitializeComponent();

        }

        public List<song> songslist { get; set; } = new List<song>();

        private void Window_Initialized(object sender, EventArgs e)
        {

        }

        void printLog(string a)
        {
            log.Text += a + "\n";
        }

        delegate void ProgressChange(int a);

        void SetProgressValue(int a) => progressBar.Value = a;

        void streamProgressCallback(object sender, StreamTransferProgressArgs args)
        {
            ProgressChange pc = new ProgressChange(SetProgressValue);
            this.Dispatcher.Invoke(pc, (int)(args.TransferredBytes * 100 / args.TotalBytes));
        }

        void downloadTo(string objectName, string downloadFilename)
        {
            try
            {
                Console.WriteLine("?"+bucketName);
                var getObjectRequest = new GetObjectRequest(bucketName, objectName);
                getObjectRequest.StreamTransferProgress += streamProgressCallback;
                // 下载文件到流。OssObject 包含了文件的各种信息，如文件所在的存储空间、文件名、元信息以及一个输入流。
                var obj = client.GetObject(getObjectRequest);
                File.WriteAllText(downloadFilename, "");
                using (var requestStream = obj.Content)
                {
                    byte[] buf = new byte[1024 * 1024];
                    var fs = File.Open(downloadFilename, FileMode.OpenOrCreate);
                    
                    var len = 0;
                    // 通过输入流将文件的内容读取到文件或者内存中。
                    while ((len = requestStream.Read(buf, 0, buf.Length)) != 0)
                    {
                        fs.Write(buf, 0, len);
                    }
                    fs.Close();
                }
                Console.WriteLine("Get object succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Get object failed. " + ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)

        {
            printLog("ksm下载器v0.3");
            printLog("设置ossutil中");
            client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            printLog("设置成功");
            printLog("要是可以的话不妨在右下角打赏几块钱来买流量QAQ");
            Refresh_list();
        }

        private void Refresh_list()
        {
            printLog("正在获取歌曲列表");
            //songlistview.ItemsSource = null;
            songslist.Clear();
            Directory.CreateDirectory(Environment.CurrentDirectory + "/ksmdownload");
            
            
            downloadTo("list.xml", Environment.CurrentDirectory + "/ksmdownload/list.xml");
            XmlDocument xml = new XmlDocument();
            xml.Load("ksmdownload/list.xml");
            XmlNodeList selectedChild = xml.SelectNodes("/songs/chart/id");
            string[] ids = new string[selectedChild.Count];
            string[] names = new string[selectedChild.Count];
            string[] levels = new string[selectedChild.Count];
            string[] artists = new string[selectedChild.Count];
            for (int i = 0; i < selectedChild.Count; i++)
            {
                ids[i] = selectedChild[i].InnerText;
                //printLog(ids[i]);
            }
            selectedChild = xml.SelectNodes("/songs/chart/name");
            for (int i = 0; i < selectedChild.Count; i++)
            {
                names[i] = selectedChild[i].InnerText;
                // printLog(names[i]);
            }
            selectedChild = xml.SelectNodes("/songs/chart/artist");
            for (int i = 0; i < selectedChild.Count; i++)
            {
                artists[i] = selectedChild[i].InnerText;
                //printLog(artists[i]);
            }
            selectedChild = xml.SelectNodes("/songs/chart/level");
            for (int i = 0; i < selectedChild.Count; i++)
            {
                levels[i] = selectedChild[i].InnerText;
                //printLog(levels[i]);
            }
            for (int i = 0; i < selectedChild.Count; i++)
            {
                songslist.Add(new song(ids[i], names[i], levels[i], artists[i]));
            }
            songlistview.ItemsSource = null;
            songlistview.ItemsSource = songslist;
            
            printLog("获取歌曲列表完成");
        }

        delegate void stringDe(string a);
        static string songExtPath = Environment.CurrentDirectory + "/songs/ksm-download/";
        static string songDownloadPath = Environment.CurrentDirectory + "/songs/";
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (songlistview.SelectedItem != null)
            {
                if (selectedSort == 0)
                {
                    songExtPath = Environment.CurrentDirectory + "/songs/ksm-download/";
                }
                else
                {
                    songExtPath = Environment.CurrentDirectory + "/songs/ksm-download-fanmade/";
                }
                Directory.CreateDirectory(songExtPath);
                song selected = (song)songlistview.SelectedItem;
                printLog("正在下载 " + selected.name);
                stringDe de = new stringDe(printLog);
                void nmsl()
                {
                    downloadTo("packages/" + selected.id + ".zip", songDownloadPath + selected.id + ".zip");
                    this.Dispatcher.Invoke(de, "下载完毕");
                    this.Dispatcher.Invoke(de, "准备解压");
                    try
                    {
                        ZipFile.ExtractToDirectory(songDownloadPath + selected.id + ".zip", songExtPath);
                    }
                    catch (Exception ex)
                    {
                        this.Dispatcher.Invoke(de, "解压出现问题:" + ex.Message);
                    }
                    //unzip("-o songs/" + selected.id + ".zip -d songs/ksm-download");
                    this.Dispatcher.Invoke(de, "解压完毕");
                    System.IO.File.Delete(songDownloadPath + selected.id + ".zip");
                    System.Media.SystemSounds.Beep.Play();
                }
                Thread a = new Thread(new ThreadStart(nmsl));

                a.Start();


            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            log.Text = "";

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Money my = new Money();
            my.Show();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("tencent://AddContact/?fromId=45&fromSubId=1&subcmd=all&uin=1323291094");
        }



        static string imgfilePath = Environment.CurrentDirectory + "/ksmdownload/imgtmp/";
        /// <summary>
        /// 载入图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void songlistview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            song selected = (song)songlistview.SelectedItem;

            if (selectedSort == 0)
            {
                imgfilePath = Environment.CurrentDirectory + "/ksmdownload/imgtmp/";
            }
            else
            {
                imgfilePath = Environment.CurrentDirectory + "/ksmdownload/imgtmp2/";
            }
            Directory.CreateDirectory(imgfilePath);
            if (selected != null)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();//初始化
                if (!File.Exists(imgfilePath + selected.id + ".jpg"))
                {
                    downloadTo("img/" + selected.id + ".jpg", imgfilePath + selected.id + ".jpg");
                }

                bmp.UriSource = new Uri(imgfilePath + selected.id + ".jpg", UriKind.Absolute);//设置图片路径

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


        int selectedSort = 0;
        /// <summary>
        /// 换sort
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void officialRadioB_Checked(object sender, RoutedEventArgs e)
        {
            bucketName = "ksm-songs";
            selectedSort = 0;
            Refresh_list();
        }

        private void fanmadeRadioB_Checked(object sender, RoutedEventArgs e)
        {
            bucketName = "ksm-fanmade";
            selectedSort = 1;
            Refresh_list();
        }
    }
}
