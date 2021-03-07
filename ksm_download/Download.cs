using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ksm_download
{
    static class Download
    {
        public static async Task<bool> downloadFile(string url, string path)
        {
            WebClient wc = new WebClient();
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

        public static async Task<T> downloadJson<T>(string url, string cookie = "", string bearer = "")
        {
            return JsonConvert.DeserializeObject<T>(await downloadText(url, cookie, bearer));
        }

        public static async Task<string> downloadText(string url,string cookie = "",string bearer = "",string usragt="")
        {

            WebClient wc = new WebClient();
            if (cookie != "") wc.Headers.Add("cookie", cookie);
            if (usragt != "") wc.Headers.Add("User-Agent", usragt);
            if (bearer != "") wc.Headers.Add("authorization", "Bearer " + bearer);

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
    }
}
