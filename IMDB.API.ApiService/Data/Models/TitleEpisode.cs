using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

public class TitleEpisode : ICSV
{
    [Key]
    public required string TConst { get; set; }

    public required string ParentTConst { get; set; }

    public int? SeasonNumber { get; set; }

    public int? EpisodeNumber { get; set; }

    public string ToCSV()
    {
        var sb = new StringBuilder();

        sb.AppendCSVField(TConst, true);
        sb.AppendCSVField(ParentTConst, true);
        sb.AppendCSVField(SeasonNumber, true);
        sb.AppendCSVField(EpisodeNumber, false);

        return sb.ToString();
    }

    public string ToHeaders() => $"{nameof(TConst)},{nameof(ParentTConst)},{nameof(SeasonNumber)},{nameof(EpisodeNumber)}";
}