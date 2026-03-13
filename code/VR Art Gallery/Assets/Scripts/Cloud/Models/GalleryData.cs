using System;
using System.Collections;
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

    [Column("artwork_map")]
    public Hashtable artwork_map { get; set; }

    [Column("created_at")]
    public DateTime created_at { get; set; }

    [Column("updated_at")]
    public DateTime updated_at { get; set; }
}
