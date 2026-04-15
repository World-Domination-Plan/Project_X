using System;
using System.Collections.Generic;
using UnityEngine;
using Postgrest.Attributes;
using Postgrest.Models;


[Table("artists")]
[Serializable]
public class ArtistProfile : BaseModel
{
    [PrimaryKey("user_id", false)] // false = not manually set, auto-generated
    public long user_id { get; set; }

    [Column("auth_user_id")]
    public string auth_user_id { get; set; }
    public DateTime created_at { get; set; }
    public string[] managed_gallery { get; set; }
    public string[] gallery_access { get; set; }
    public string username { get; set; }

    public ArtistProfile()
    {
        created_at = DateTime.UtcNow;
    }
}

