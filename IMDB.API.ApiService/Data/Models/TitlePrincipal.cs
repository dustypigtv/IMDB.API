using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TConst), nameof(Ordering), nameof(NConst))]
public class TitlePrincipal : ICSV
{
    [Key]
    public required string TConst { get; set; }

    public int Ordering { get; set; }

    public required string NConst { get; set; }

    public required string Category { get; set; }

    public string? Job { get; set; }

    public string? Character { get; set; }

    public string ToCSV()
    {
        var sb = new StringBuilder();

        sb.AppendCSVField(TConst, true);
        sb.AppendCSVField(Ordering, true);
        sb.AppendCSVField(NConst, true);
        sb.AppendCSVField(Category, true);
        sb.AppendCSVField(Job, true);
        sb.AppendCSVField(Character, false);

        return sb.ToString();
    }

    public string ToHeaders() => $"{nameof(TConst)},{nameof(Ordering)},{nameof(NConst)},{nameof(Category)},{nameof(Job)},{nameof(Character)}";
}