using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class TitleBasic
{
    [JsonIgnore]
    [Key]
    public ulong TConstId { get; set; }

    [NotMapped]
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