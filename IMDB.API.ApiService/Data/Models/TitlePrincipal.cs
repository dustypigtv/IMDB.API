using Microsoft.EntityFrameworkCore;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TConst), nameof(Ordering), nameof(NConst))]
public class TitlePrincipal
{
    public string TConst { get; set; } = string.Empty;

    public ushort Ordering { get; set; }

    public string NConst { get; set; } = string.Empty;

    public required string Category { get; set; }

    public string? Job { get; set; }

    public string? Character { get; set; }
}