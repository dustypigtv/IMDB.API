using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleRating
{
    [Key]
    public string TConst { get; set; } = string.Empty;

    public float AverageWeighting { get; set; }

    public uint NumVotes { get; set; }
}