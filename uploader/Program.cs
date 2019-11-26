using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Drawing;
using Aliyun.OSS;
using System.IO.Compression;
using System.Drawing.Imaging;

namespace uploader
{
    public class song
    {
        public string id { get; set; }
        public string name { get; set; }
        public string level { get; set; }
        public string artist { get; set; }
        public song(string _id, string _name, string _lv, string _art)
        {
            id = _id;
            name = _name;
            level = _lv;
            artist = _art;
        }
    }
    class Program
    {
        static string endpoint = "oss-cn-shanghai.aliyuncs.com";
        static string accessKeyId = "LTAIGTuicNd5bdm3";
        static string accessKeySecret = "hxsgrUuol43h2vwBIoyckb3Bhz91Dv";
        static string bucketName = "ksm-songs";

        static List<song> songslist { get; set; } = new List<song>();
        static List<song> newsongslist { get; set; } = new List<song>();

        static void downloadTo(string objectName, string downloadFilename)
        {
            Console.WriteLine("downloading");
            OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            var obj = client.GetObject(bucketName, objectName);
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

        }

        static void uploadTo(string localFilename,string objectName)
        {
            try
            {
                // 上传文件。
                OssClient client = new OssClient(endpoint, accessKeyId, accessKeySecret);
                client.PutObject(bucketName, objectName, localFilename);
                Console.WriteLine("Put object succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Put object failed, {0}", ex.Message);
            }
        }

        static void refresh_list()
        {
            Console.WriteLine("正在获取现有歌曲列表");
            songslist.Clear();
            System.IO.File.Delete("list.xml");
            downloadTo("list.xml", Environment.CurrentDirectory + "/list.xml");
            XmlDocument xml = new XmlDocument();
            
            xml.Load(Environment.CurrentDirectory + "/list.xml");
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
                Console.WriteLine("id: {0}     Level:{1}     Name:{2} - {3}", ids[i], levels[i], names[i], artists[i]);
            }
            Console.WriteLine("获取歌曲列表完成");
        }

        static DirectoryInfo[] songDirs;
        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }
        static void scan_songs()
        {
            Directory.CreateDirectory(Environment.CurrentDirectory + "/img");
            int lastId = int.Parse(songslist[songslist.Count - 1].id);
            //Console.WriteLine(lastId);
            DirectoryInfo dirInfo = new DirectoryInfo(Environment.CurrentDirectory + "/songs");
            //Console.WriteLine(dirInfo.FullName);
            songDirs = dirInfo.GetDirectories();
            for (int i = 0; i < songDirs.Length; i++)
            {
                FileInfo[] songFiles = songDirs[i].GetFiles();
                string songName = "", songArtist = "";
                string[] levels = new string[4];
                Bitmap bg = new Bitmap(1,1);
                var eps = new EncoderParameters(1);
                var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                for (int j = 0; j < songFiles.Length; j++)
                {
                    string difficulty = "", level = "";
                    //Console.WriteLine(songFiles[j].Extension);
                    switch (songFiles[j].Extension)
                    {
                        case ".ksh":
                            string[] kshFile = File.ReadAllText(songFiles[j].FullName).Split('\n');

                            for (int a = 0; a < 20; a++)//只读前20行
                            {
                                String[] line = kshFile[a].Replace('\r', '=').Split('=');

                                switch (line[0])
                                {
                                    case "title":
                                        songName = line[1];
                                        break;
                                    case "artist":
                                        songArtist = line[1];
                                        break;
                                    case "difficulty":
                                        difficulty = line[1];
                                        break;
                                    case "level":
                                        level = line[1];
                                        break;
                                    default:
                                        break;
                                }

                            }
                            switch (difficulty)
                            {
                                case "light":
                                    levels[0] = level;
                                    break;
                                case "challenge":
                                    levels[1] = level;
                                    break;
                                case "extended":
                                    levels[2] = level;
                                    break;
                                case "infinite":
                                    levels[3] = level;
                                    break;
                                default:
                                    levels[3] = level;
                                    break;
                            }
                            break;
                        case ".png":
                            Stream fs = new FileStream(songFiles[j].FullName,FileMode.Open,FileAccess.Read);
                            bg = new Bitmap(fs);
                            fs.Close();
                            break;
                        case ".jpg":
                            Stream fs2 = new FileStream(songFiles[j].FullName, FileMode.Open, FileAccess.Read);
                            bg = new Bitmap(fs2);
                            fs2.Close();
                            break;
                        default:
                            break;
                    }

                }
                lastId++;
                song thisSong = new song(lastId.ToString(), songName, levels[0] + "/" + levels[1] + "/" + levels[2] + "/" + levels[3], songArtist);
                Console.WriteLine("ksh read ,id {0} \t level {2} \t name:{1} ", thisSong.id, thisSong.name, thisSong.level);
                newsongslist.Add(thisSong);
                string imgurl = Environment.CurrentDirectory + "/img/"+thisSong.id+".jpg";
                eps.Param[0] = ep;
                var jpsEncodeer = GetEncoder(ImageFormat.Jpeg);
                //保存图片
                bg = new Bitmap(bg, new Size(250, 250));
                bg.Save(imgurl, jpsEncodeer, eps);
                bg.Dispose();
                eps.Dispose();
                ep.Dispose();
            }
        }

        static void xml_update()
        {
            XmlDocument xml = new XmlDocument();

            xml.Load(Environment.CurrentDirectory + "/list.xml");
            XmlNode root = xml.SelectSingleNode("songs");
            for(int i = 0; i < newsongslist.Count; i++)
            {
                XmlElement xmlE = xml.CreateElement("chart");

                XmlElement xmlId = xml.CreateElement("id");
                xmlId.InnerText = newsongslist[i].id;
                xmlE.AppendChild(xmlId);

                XmlElement xmlName = xml.CreateElement("name");
                xmlName.InnerText = newsongslist[i].name;
                xmlE.AppendChild(xmlName);
                
                XmlElement xmlArtist = xml.CreateElement("artist");
                xmlArtist.InnerText = newsongslist[i].artist;
                xmlE.AppendChild(xmlArtist);

                XmlElement xmlLevel = xml.CreateElement("level");
                xmlLevel.InnerText = newsongslist[i].level;
                xmlE.AppendChild(xmlLevel);

                root.AppendChild(xmlE);
            }
            Console.WriteLine("Xml Appended");
            Console.WriteLine("{0} new songs",newsongslist.Count);
            xml.Save(Environment.CurrentDirectory + "/list.xml");
        }


        static void zipping()
        {
            Console.Write("           正在打包...");
            
                Directory.CreateDirectory(Environment.CurrentDirectory + "/packages");
                Directory.CreateDirectory(Environment.CurrentDirectory + "/packages/tmp");
                for (int i = 0; i < songDirs.Length; i++)
                {
                    songDirs[i].MoveTo(Environment.CurrentDirectory + "/packages/tmp/" + newsongslist[i].id);
                    ZipFile.CreateFromDirectory(Environment.CurrentDirectory + "/packages/tmp/", Environment.CurrentDirectory + "/packages/" + newsongslist[i].id + ".zip");
                    //songDirs[i].MoveTo(Environment.CurrentDirectory + "/songs/" + songDirs[i].Name);

                    Directory.Delete(Environment.CurrentDirectory + "/packages/tmp/" + newsongslist[i].id + "/", true);
                    Console.Write("\r{0}/{1}",i+1, songDirs.Length);
                }
                Directory.Delete(Environment.CurrentDirectory + "/packages/tmp/", true);
            
        }

        static void UploadFiles()
        {
            Console.WriteLine("正在上传:Xml");
            uploadTo(Environment.CurrentDirectory + "/list.xml", "list.xml");
            DirectoryInfo zipdirinfo = new DirectoryInfo(Environment.CurrentDirectory + "/packages");
            FileInfo[] zips = zipdirinfo.GetFiles();
            for(int i = 0; i < zips.Length; i++)
            {
                Console.WriteLine("正在上传:"+ zips[i].Name);
                uploadTo(zips[i].FullName, "packages/"+zips[i].Name);
            }
            DirectoryInfo imgdirinfo = new DirectoryInfo(Environment.CurrentDirectory + "/img");
            FileInfo[] imgs = imgdirinfo.GetFiles();
            for (int i = 0; i < imgs.Length; i++)
            {
                Console.WriteLine("正在上传:" + imgs[i].Name);
                uploadTo(imgs[i].FullName, "img/" + imgs[i].Name);
            }
        }
        static void Main(string[] args)
        {
            try
            {
                Directory.Delete(Environment.CurrentDirectory + "/packages", true);
            }
            catch { }
            try
            {
                Directory.Delete(Environment.CurrentDirectory + "/img", true);
            }
            catch { }
            string arg;
            Console.WriteLine("Where to upload?fanmade or official?(f/o)");
            arg = Console.ReadLine();
            if (arg == "f" || arg == "F")
                bucketName = "ksm-fanmade";
            else if (arg == "o" || arg == "O")
                bucketName = "ksm-songs";
            else
                return;

            refresh_list();
            scan_songs();
            xml_update();
            
            Console.WriteLine("将开始打包上传，songs文件夹内内容将会消失，继续?(y/n)");
            
            if (Console.ReadLine() == "y"|| Console.ReadLine() == "Y")
            {   
                zipping();//封包zip
                UploadFiles();
                //上传文件
                Directory.Delete(Environment.CurrentDirectory + "/packages", true);
                Directory.Delete(Environment.CurrentDirectory + "/img", true);
                Console.WriteLine("操作已完成");
            }
            else
            {
                Console.WriteLine("操作已取消");
            }
            Console.ReadKey();
        }
    }
}
