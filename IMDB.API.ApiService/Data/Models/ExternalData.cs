using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class ExternalData
{
    [Key]
    public required string TConst { get; set; }

    public DateOnly? Date { get; set; }

    public string? Plot { get; set; }

    public string? ImageUrl { get; set; }

    [JsonPropertyName("mpaaRating")]
    public string? MPAA_Rating { get; set; }

    public DateTime LastUpdated { get; set; }
}
