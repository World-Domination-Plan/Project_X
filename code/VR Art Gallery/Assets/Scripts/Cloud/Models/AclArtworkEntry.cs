using System;
using Postgrest.Attributes;
using Postgrest.Models;

[Table("acl_artwork")]
[Serializable]
public class AclArtworkEntry : BaseModel
{
    [PrimaryKey("id")]
    public long id { get; set; }

    public int artwork_id { get; set; }
    public int artist_id { get; set; }
    public string status { get; set; }
}
