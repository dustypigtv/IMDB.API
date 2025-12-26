using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TConst), nameof(Ordering), nameof(Title))]
public class TitleAka : ICSV
{
    [Key]
    public required string TConst { get; set; }

    public int Ordering { get; set; }

    public required string Title { get; set; }

    public string? Region { get; set; }

    public string? Language { get; set; }

    public List<string>? Types { get; set; }

    public List<string>? Attributes { get; set; }

    public bool IsOriginalTitle { get; set; }

    public string ToCSV()
    {
        var sb = new StringBuilder();

        sb.AppendCSVField(TConst, true);
        sb.AppendCSVField(Ordering, true);
        sb.AppendCSVField(Title, true);
        sb.AppendCSVField(Region, true);
        sb.AppendCSVField(Language, true);
        sb.AppendCSVField(Types, true);
        sb.AppendCSVField(Attributes, true);
        sb.AppendCSVField(IsOriginalTitle, false);

        return sb.ToString();
    }

    public string ToHeaders() => $"{nameof(TConst)},{nameof(Ordering)},{nameof(Title)},{nameof(Region)},{nameof(Language)},{nameof(Types)},{nameof(Attributes)},{nameof(IsOriginalTitle)}";
}