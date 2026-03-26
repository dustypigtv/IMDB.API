using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class NameBasic
{
    [Key]
    public string NConst { get; set; } = string.Empty;

    public required string PrimaryName { get; set; }

    public ushort? BirthYear { get; set; }

    public ushort? DeathYear { get; set; }

    public List<string>? PrimaryProfessions { get; set; }

    public List<string>? KnownForTitles { get; set; }
}