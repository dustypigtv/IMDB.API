using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleBasic
{
    [Key]
    public string TConst { get; set; } = string.Empty;

    public required string TitleType { get; set; }

    public required string PrimaryTitle { get; set; }

    public required string OriginalTitle { get; set; }

    public bool IsAdult { get; set; }

    public ushort? StartYear { get; set; }

    public ushort? EndYear { get; set; }

    public ushort? RuntimeMinutes { get; set; }

    public List<string>? Genres { get; set; }
}