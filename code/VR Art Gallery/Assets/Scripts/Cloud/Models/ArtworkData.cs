using System;
using System.Collections.Generic;
using UnityEngine;
using Postgrest.Attributes;
using Postgrest.Models;


[Table("artwork")]
[Serializable]
public class ArtworkData : BaseModel
{
    [PrimaryKey("id")]
    public int id { get; set; }
    
    [Column("title")]
    public string title { get; set; }

    [Column("owner_id")]
    public int owner_id { get; set; }
  
    // Image storage
    [Column("image_url")]
    public string image_url { get; set; }        // Full resolution artwork

    [Column("thumbnail_url")]
    public string thumbnail_url { get; set; }    // Small preview (512x512)
    
    // Metadata
    [Column("filesize_bytes")]
    public long filesize_bytes { get; set; }
    
    // Timestamps
    [Column("created_at")]
    public DateTime created_at { get; set; }

    [Column("updated_at")]
    public DateTime updated_at { get; set; }
    
    public ArtworkData()
    {
        created_at = DateTime.UtcNow;
        updated_at = DateTime.UtcNow;
    }
}
