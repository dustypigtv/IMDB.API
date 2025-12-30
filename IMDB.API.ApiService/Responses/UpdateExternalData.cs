using IMDB.API.ApiService.Data.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMDB.API.ApiService.Responses;

public class UpdateExternalData : ExternalData
{
    [NotMapped]
    public string? TitleType { get; set; }

    [NotMapped]
    public bool HasEpisodes { get; set; }
}
