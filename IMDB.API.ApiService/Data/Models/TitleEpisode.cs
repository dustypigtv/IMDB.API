using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class TitleEpisode
{
    [JsonIgnore]
    [Key]
    public ulong TConstId { get; set; }

    [NotMapped]
    public string TConst { get; set; } = string.Empty;

    [JsonIgnore]
    public ulong ParentTConstId { get; set; }

    [NotMapped]
    public string ParentTConst { get; set; } = string.Empty;

    public ushort? SeasonNumber { get; set; }

    public ushort? EpisodeNumber { get; set; }
}