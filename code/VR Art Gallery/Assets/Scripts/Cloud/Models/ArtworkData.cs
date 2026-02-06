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
    public string id { get; set; }
    
    public string title { get; set; }
    public string ownerUserId { get; set; }
  
    // Image storage
    public string imageUrl { get; set; }        // Full resolution artwork
    public string thumbnailUrl { get; set; }    // Small preview (512x512)
    
    // Metadata
    public long fileSizeBytes { get; set; }
    
    // Contributors (people who worked on this during session)
    public List<string> contributorUserIds { get; set; }
    
    // Timestamps
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    
    public ArtworkData()
    {
        id = Guid.NewGuid().ToString();
        contributorUserIds = new List<string>();
        createdAt = DateTime.UtcNow;
        updatedAt = DateTime.UtcNow;
    }
}
