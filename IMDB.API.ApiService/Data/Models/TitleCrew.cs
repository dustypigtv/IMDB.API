using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class TitleCrew
{
    [JsonIgnore]
    [Key]
    public ulong TConstId { get; set; }

    [NotMapped]
    public string TConst { get; set; } = string.Empty;

    public List<string>? Directors { get; set; }

    public List<string>? Writers { get; set; }
}