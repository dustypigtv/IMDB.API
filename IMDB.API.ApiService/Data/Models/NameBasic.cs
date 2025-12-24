using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class NameBasic : IEquatable<NameBasic?>
{
    [Key]
    public required string NConst { get; set; }

    public required string PrimaryName { get; set; }

    public int? BirthYear { get; set; }

    public int? DeathYear { get; set; }

    public List<string>? PrimaryProfessions { get; set; }

    public List<string>? KnownForTitles { get; set; }



    public override bool Equals(object? obj)
    {
        return Equals(obj as NameBasic);
    }

    public bool Equals(NameBasic? other)
    {
        return other is not null &&
               NConst == other.NConst &&
               PrimaryName == other.PrimaryName &&
               BirthYear == other.BirthYear &&
               DeathYear == other.DeathYear &&
               (PrimaryProfessions ?? []).SequenceEqual(other.PrimaryProfessions ?? []) &&
               (KnownForTitles ?? []).SequenceEqual(other.KnownForTitles ?? []);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NConst, PrimaryName, BirthYear, DeathYear, PrimaryProfessions, KnownForTitles);
    }

    public static bool operator ==(NameBasic? left, NameBasic? right)
    {
        return EqualityComparer<NameBasic>.Default.Equals(left, right);
    }

    public static bool operator !=(NameBasic? left, NameBasic? right)
    {
        return !(left == right);
    }
}
