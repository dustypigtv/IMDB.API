using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

public class TitleBasic : ICSV
{
    [Key]
    public required string TConst { get; set; }

    public required string TitleType { get; set; }

    public required string PrimaryTitle { get; set; }

    public required string OriginalTitle { get; set; }

    public bool IsAdult { get; set; }

    public ushort? StartYear { get; set; }

    public ushort? EndYear { get; set; }

    public uint? RuntimeMinutes { get; set; }

    public List<string>? Genres { get; set; }

    public string ToCSV()
    {
        var sb = new StringBuilder();

        sb.AppendCSVField(TConst, true);
        sb.AppendCSVField(TitleType, true);
        sb.AppendCSVField(PrimaryTitle, true);
        sb.AppendCSVField(OriginalTitle, true);
        sb.AppendCSVField(IsAdult, true);
        sb.AppendCSVField(StartYear, true);
        sb.AppendCSVField(EndYear, true);
        sb.AppendCSVField(RuntimeMinutes, true);
        sb.AppendCSVField(Genres, false);

        return sb.ToString();
    }

    public string ToHeaders() => $"{nameof(TConst)},{nameof(TitleType)},{nameof(PrimaryTitle)},{nameof(OriginalTitle)},{nameof(IsAdult)},{nameof(StartYear)},{nameof(EndYear)},{nameof(RuntimeMinutes)},{nameof(Genres)}";

}