using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TConst), nameof(Ordering), nameof(TitleHashId))]
public class TitleAka
{
    public string TConst { get; set; } = string.Empty;

    public ushort Ordering { get; set; }

    [JsonIgnore]
    public long TitleHashId { get; set; }

    public required string Title { get; set; }

    public string? Region { get; set; }

    public string? Language { get; set; }

    public List<string>? Types { get; set; }

    public List<string>? Attributes { get; set; }

    public bool IsOriginalTitle { get; set; }
}