using System;
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using UnityEngine;

[Table("artists")]
[Serializable]
public class ArtistProfile : BaseModel
{
    [PrimaryKey("user_id", false)]
    [Column("user_id")]
    [JsonProperty("user_id")]
    public long id { get; set; }

    [Column("auth_user_id")]
    [JsonProperty("auth_user_id")]
    public string auth_user_id { get; set; }

    [Column("username")]
    [JsonProperty("username")]
    public string username { get; set; }

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime created_at { get; set; }

    [Column("managed_gallery")]
    [JsonProperty("managed_gallery")]
    public string[] managed_gallery { get; set; }

    [Column("gallery_access")]
    [JsonProperty("gallery_access")]
    public string[] gallery_access { get; set; }

    public ArtistProfile()
    {
        created_at = DateTime.UtcNow;
    }
}
