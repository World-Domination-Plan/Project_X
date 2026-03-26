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
    
    public string title { get; set; }
    public long owner_id { get; set; }
  
    // Image storage
    public string image_url { get; set; }        // Full resolution artwork
    public string thumbnail_url { get; set; }    // Small preview (512x512)
    
    // Metadata
    public long filesize_bytes { get; set; }
    
    // Timestamps
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    
    public ArtworkData()
    {
        created_at = DateTime.UtcNow;
        updated_at = DateTime.UtcNow;
    }
}
