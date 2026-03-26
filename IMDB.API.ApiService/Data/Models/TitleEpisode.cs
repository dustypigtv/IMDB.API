using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleEpisode
{
    [Key]
    public string TConst { get; set; } = string.Empty;

    public string ParentTConst { get; set; } = string.Empty;

    public ushort? SeasonNumber { get; set; }

    public ushort? EpisodeNumber { get; set; }
}