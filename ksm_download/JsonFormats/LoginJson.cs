using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksm_download.JsonFormats
{
    public class LoginJson
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
        public LoginJson(string _u,string _p)
        {
            Username = _u;
            Password = _p;
        }
    }

    public partial class UserJsonRoot
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("data")]
        public UserJsonData Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

    public partial class UserJsonData
    {
        [JsonProperty("availableDownloadTimes")]
        public long AvailableDownloadTimes { get; set; }

        [JsonProperty("downloadedSize")]
        public long DownloadedSize { get; set; }

        [JsonProperty("permissions")]
        public long Permissions { get; set; }

        [JsonProperty("registerTime")]
        public long RegisterTime { get; set; }

        [JsonProperty("username")]
        public string Usrname { get; set; }
    }
}
