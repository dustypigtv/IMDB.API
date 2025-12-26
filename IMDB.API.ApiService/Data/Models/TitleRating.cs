using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

public class TitleRating : ICSV
{
    [Key]
    public required string TConst { get; set; }

    public float AverageWeighting { get; set; }

    public int NumVotes { get; set; }

    public string ToCSV()
    {
        var sb = new StringBuilder();

        sb.AppendCSVField(TConst, true);
        sb.AppendCSVField(AverageWeighting, true);
        sb.AppendCSVField(NumVotes, false);

        return sb.ToString();
    }

    public string ToHeaders() => $"{nameof(TConst)},{nameof(AverageWeighting)},{nameof(NumVotes)}";
}