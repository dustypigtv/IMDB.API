using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IMDB.API.ApiService.Data.Models;

public class TitleRating
{
    [JsonIgnore]
    [Key]
    public ulong TConstId { get; set; }

    [NotMapped]
    public string TConst { get; set; } = string.Empty;

    public float AverageWeighting { get; set; }

    public uint NumVotes { get; set; }
}