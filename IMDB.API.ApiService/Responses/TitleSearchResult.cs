using IMDB.API.ApiService.Data.Models;

namespace IMDB.API.ApiService.Responses;

public class TitleSearchResult
{
    public required TitleBasic Basic { get; set; }

    public TitleRating? Rating { get; set; }

    public float Rank { get; set; }
}
