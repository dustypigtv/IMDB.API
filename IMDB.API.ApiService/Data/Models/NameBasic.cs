using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

public class NameBasic : ICSV
{
    [Key]
    public required string NConst { get; set; }

    public required string PrimaryName { get; set; }

    public int? BirthYear { get; set; }

    public int? DeathYear { get; set; }

    public List<string>? PrimaryProfessions { get; set; }

    public List<string>? KnownForTitles { get; set; }

    public string ToHeaders() => $"{nameof(NConst)},{nameof(PrimaryName)},{nameof(BirthYear)},{nameof(DeathYear)},{nameof(PrimaryProfessions)},{nameof(KnownForTitles)}";

    public string ToCSV()
    {
        StringBuilder sb = new();
        sb.AppendCSVField(NConst, true);
        sb.AppendCSVField(PrimaryName, true);
        sb.AppendCSVField(BirthYear, true);
        sb.AppendCSVField(DeathYear, true);
        sb.AppendCSVField(PrimaryProfessions, true);
        sb.AppendCSVField(KnownForTitles, false);

        return sb.ToString();
    }
}