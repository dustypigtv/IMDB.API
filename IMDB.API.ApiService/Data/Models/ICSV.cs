namespace IMDB.API.ApiService.Data.Models;

public interface ICSV
{
    string ToHeaders();
    string ToCSV();
}
