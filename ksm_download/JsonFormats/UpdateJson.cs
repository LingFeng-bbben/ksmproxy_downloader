using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksm_download.JsonFormats
{
    using Newtonsoft.Json;

    public partial class UpdateJson
    {
        [JsonProperty("version")]
        public float Version { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }
    }

}
