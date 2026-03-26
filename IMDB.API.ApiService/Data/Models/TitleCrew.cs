using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleCrew
{
    [Key]
    public string TConst { get; set; } = string.Empty;

    public List<string>? Directors { get; set; }

    public List<string>? Writers { get; set; }
}