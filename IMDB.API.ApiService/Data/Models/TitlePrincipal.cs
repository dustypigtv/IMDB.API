using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TConst), nameof(Ordering), nameof(NConst))]
public class TitlePrincipal : IEquatable<TitlePrincipal?>
{
    [Key]
    public required string TConst { get; set; }

    public int Ordering { get; set; }

    public required string NConst { get; set; }

    public required string Category { get; set; }

    public string? Job { get; set; }

    public string? Character { get; set; }

    public string DictKey() => $"{TConst}.{Ordering}.{NConst}";

    public override bool Equals(object? obj)
    {
        return Equals(obj as TitlePrincipal);
    }

    public bool Equals(TitlePrincipal? other)
    {
        return other is not null &&
               TConst == other.TConst &&
               Ordering == other.Ordering &&
               NConst == other.NConst &&
               Category == other.Category &&
               Job == other.Job &&
               Character == other.Character;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TConst, Ordering, NConst, Category, Job, Character);
    }

    public static bool operator ==(TitlePrincipal? left, TitlePrincipal? right)
    {
        return EqualityComparer<TitlePrincipal>.Default.Equals(left, right);
    }

    public static bool operator !=(TitlePrincipal? left, TitlePrincipal? right)
    {
        return !(left == right);
    }
}