using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TConstId), nameof(Ordering), nameof(NConstId))]
public class TitlePrincipal
{
    [JsonIgnore]
    public ulong TConstId { get; set; }

    [NotMapped]
    public string TConst { get; set; } = string.Empty;

    public ushort Ordering { get; set; }

    [JsonIgnore]
    public ulong NConstId { get; set; }

    [NotMapped]
    public string NConst { get; set; } = string.Empty;

    public required string Category { get; set; }

    public string? Job { get; set; }

    public string? Character { get; set; }
}