using IMDB.API.ApiService.Data.Models;

namespace IMDB.API.ApiService.Responses;

public class UpdateExternalData : ExternalData
{
    public required string TitleType { get; set; }

    public bool HasEpisodes { get; set; }
}
