using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ArtworkData
{
    public string id;
    public string title;
    public string ownerUserId;
    public string galleryId;
    
    // Image storage
    public string imageUrl;        // Full resolution artwork
    public string thumbnailUrl;    // Small preview (512x512)
    
    // Metadata
    public int width;
    public int height;
    public long fileSizeBytes;
    
    // Contributors (people who worked on this during session)
    public List<string> contributorUserIds;
    
    // Timestamps
    public DateTime createdAt;
    public DateTime updatedAt;
    
    public ArtworkData()
    {
        id = Guid.NewGuid().ToString();
        contributorUserIds = new List<string>();
        width = 2048;
        height = 2048;
        createdAt = DateTime.UtcNow;
        updatedAt = DateTime.UtcNow;
    }
}
