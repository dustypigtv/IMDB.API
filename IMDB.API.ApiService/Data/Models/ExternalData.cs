using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class ExternalData
{
    [JsonIgnore]
    [Key]
    public ulong TConstId { get; set; }

    [NotMapped]
    public string TConst { get; set; } = string.Empty;

    public DateOnly? Date { get; set; }

    public string? Plot { get; set; }

    public string? ImageUrl { get; set; }

    [JsonPropertyName("mpaaRating")]
    public string? MPAA_Rating { get; set; }

    [JsonIgnore]
    public DateTime LastUpdated { get; set; }
}
