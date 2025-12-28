using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class NameBasic
{
    [JsonIgnore]
    [Key]
    public ulong NConstId { get; set; }

    [NotMapped]
    public string NConst { get; set; } = string.Empty;

    public required string PrimaryName { get; set; }

    public ushort? BirthYear { get; set; }

    public ushort? DeathYear { get; set; }

    public List<string>? PrimaryProfessions { get; set; }

    public List<string>? KnownForTitles { get; set; }
}