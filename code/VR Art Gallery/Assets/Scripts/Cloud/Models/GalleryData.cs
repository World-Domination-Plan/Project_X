using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

[Table("gallery")]
public class GalleryData : BaseModel
{
    [PrimaryKey("id", false)]
    public int id { get; set; }

    [Column("name")]
    public string name { get; set; }

    [Column("description")]
    public string description { get; set; }

    [Column("owner_id")]
    public string owner_id { get; set; }

    /// <summary>
    /// Flat list of all artwork IDs inside this gallery.
    /// Use AddArtwork / RemoveArtwork to mutate safely.
    /// </summary>
    [Column("artwork_ids")]
    public List<int> artwork_ids { get; set; } = new List<int>();

    /// <summary>
    /// Maps gallery slot index → artwork ID.
    /// Defines which artwork occupies which physical slot in the gallery space.
    /// </summary>
    [Column("artwork_map")]
    public Dictionary<int, int> artwork_map { get; set; } = new Dictionary<int, int>();

    [Column("created_at")]
    public DateTime created_at { get; set; }

    [Column("updated_at")]
    public DateTime updated_at { get; set; }

    // ── Convenience helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Adds an artwork ID to the gallery inventory if not already present.
    /// </summary>
    public void AddArtwork(int artworkId)
    {
        if (!artwork_ids.Contains(artworkId))
            artwork_ids.Add(artworkId);
    }

    /// <summary>
    /// Removes an artwork ID from the gallery inventory and clears
    /// any slot it was occupying in the artwork_map.
    /// </summary>
    public void RemoveArtwork(int artworkId)
    {
        artwork_ids.Remove(artworkId);

        var keysToRemove = new List<int>();
        foreach (var kvp in artwork_map)
            if (kvp.Value == artworkId)
                keysToRemove.Add(kvp.Key);
        foreach (var key in keysToRemove)
            artwork_map.Remove(key);
    }

    /// <summary>
    /// Assigns an artwork to a specific slot. Adds to artwork_ids automatically
    /// if not already present.
    /// </summary>
    public void PlaceArtworkInSlot(int slotIndex, int artworkId)
    {
        AddArtwork(artworkId);
        artwork_map[slotIndex] = artworkId;
    }
}
