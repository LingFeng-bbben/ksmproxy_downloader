using Newtonsoft.Json;
using System;

namespace ksm_download.JsonFormats
{
    public class SongListJsonRoot
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public SongListJson Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
    public partial class SongListJson
    {
        [JsonProperty("data")]
        public Datum[] Data { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

    public partial class Datum
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("user_id")]
        public Guid UserId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("jacket_filename")]
        public string JacketFilename { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("download_link")]
        public object DownloadLink { get; set; }

        [JsonProperty("downloads")]
        public long Downloads { get; set; }

        [JsonProperty("has_preview")]
        public long HasPreview { get; set; }

        [JsonProperty("hidden")]
        public long Hidden { get; set; }

        [JsonProperty("mojibake")]
        public long Mojibake { get; set; }

        [JsonProperty("uploaded_at")]
        public DateTimeOffset UploadedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("jacket_url")]
        public Uri JacketUrl { get; set; }

        [JsonProperty("preview_url")]
        public Uri PreviewUrl { get; set; }

        [JsonProperty("cdn_download_url")]
        public Uri CdnDownloadUrl { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("charts")]
        public Chart[] Charts { get; set; }

        [JsonProperty("tags")]
        public Tag[] Tags { get; set; }
    }

    public partial class Chart
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("user_id")]
        public Guid UserId { get; set; }

        [JsonProperty("song_id")]
        public Guid SongId { get; set; }

        [JsonProperty("difficulty")]
        public long Difficulty { get; set; }

        [JsonProperty("level")]
        public long Level { get; set; }

        [JsonProperty("effector")]
        public string Effector { get; set; }

        [JsonProperty("video_link")]
        public Uri VideoLink { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public partial class Tag
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("song_id")]
        public Guid SongId { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public partial class User
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("urlRoute")]
        public string UrlRoute { get; set; }

        [JsonProperty("twitter")]
        public string Twitter { get; set; }

        [JsonProperty("youtube")]
        public Uri Youtube { get; set; }

        [JsonProperty("bio")]
        public string Bio { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("songCount")]
        public long SongCount { get; set; }
    }


    public partial class Meta
    {
        [JsonProperty("current_page")]
        public long CurrentPage { get; set; }

        [JsonProperty("from")]
        public long From { get; set; }

        [JsonProperty("last_page")]
        public long LastPage { get; set; }

        [JsonProperty("path")]
        public Uri Path { get; set; }

        [JsonProperty("per_page")]
        public long PerPage { get; set; }

        [JsonProperty("to")]
        public long To { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }

}
