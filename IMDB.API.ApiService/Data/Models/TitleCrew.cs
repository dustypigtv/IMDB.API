using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

public class TitleCrew : ICSV
{
    [Key]
    public required string TConst { get; set; }

    public List<string>? Directors { get; set; }

    public List<string>? Writers { get; set; }

    public string ToCSV()
    {
        var sb = new StringBuilder();

        sb.AppendCSVField(TConst, true);
        sb.AppendCSVField(Directors, true);
        sb.AppendCSVField(Writers, false);

        return sb.ToString();
    }

    public string ToHeaders() => $"{nameof(TConst)},{nameof(Directors)},{nameof(Writers)}";
}