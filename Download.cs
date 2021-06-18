using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using ksm_download.JsonFormats;

namespace ksm_download
{
    static class Download
    {
        public static async Task<bool> downloadFile(string url, string path, Dictionary<string, string> keypair = null)
        {
            WebClient wc = new WebClient();
            if (keypair != null)
            {
                foreach (var item in keypair)
                {
                    wc.Headers.Add(item.Key, item.Value);
                }
            }
            Console.WriteLine("downloading");
            try
            {
                byte[] data = await wc.DownloadDataTaskAsync(url);
                Console.WriteLine("saving");
                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("failed." + e.Message);
                return false;
            }
        }

        public static async Task<T> downloadJson<T>(string url, Dictionary<string, string> keypair = null)
        {
            return JsonConvert.DeserializeObject<T>(await downloadText(url, keypair));
        }

        public static async Task<string> downloadText(string url,Dictionary<string,string> keypair = null)
        {
            WebClient wc = new WebClient();
            if (keypair != null)
            {
                foreach(var item in keypair)
                {
                    wc.Headers.Add(item.Key, item.Value);
                }
            }

            Console.WriteLine("downloading " + url);
            try
            {
                byte[] data = await wc.DownloadDataTaskAsync(url);
                Console.WriteLine("saving");
                string text = Encoding.UTF8.GetString(data);
                return text;
            }
            catch (Exception e)
            {
                Console.WriteLine("failed." + e.Message);
                return default;
            }
        }

        public static async Task<string> login(string url, string username,string password)
        {
            WebClient wc = new WebClient();
            try
            {
                string jsondata = JsonConvert.SerializeObject(new LoginJson(username, password));
                byte[] buffer = Encoding.UTF8.GetBytes(jsondata);
                byte[] data = await wc.UploadDataTaskAsync(url, "POST", buffer);
                string text = Encoding.UTF8.GetString(data);
                SongListJsonRoot js = JsonConvert.DeserializeObject<SongListJsonRoot>(text);
                if (js.Code != 200)
                {
                    return js.Code.ToString();
                }
                return wc.ResponseHeaders.Get("Set-Cookie");
            }
            catch (Exception e)
            {
                Console.WriteLine("failed." + e.Message);
                return default;
            }
            
        }
    }
}
