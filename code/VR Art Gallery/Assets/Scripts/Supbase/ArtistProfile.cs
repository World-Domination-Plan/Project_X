using System;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace VRGallery.Cloud.Models
{
    [Table("artists")]
    public class ArtistProfile : BaseModel
    {
        [PrimaryKey("user_id", false)]
        [Column("user_id")]
        public string user_id { get; set; }

        [Column("created_at")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? created_at { get; set; }

        [Column("username")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string username { get; set; }

        // json[] columns: easiest is object[] / dynamic-ish
        [Column("managed_gallery")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object[] managed_gallery { get; set; }

        [Column("gallery_access")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object[] gallery_access { get; set; }
    }
}
