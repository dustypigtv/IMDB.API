using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleCrew : IEquatable<TitleCrew?>
{
    [Key]
    public required string TConst { get; set; }

    public List<string>? Directors { get; set; }

    public List<string>? Writers { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TitleCrew);
    }

    public bool Equals(TitleCrew? other)
    {
        return other is not null &&
               TConst == other.TConst &&
               (Directors ?? []).SequenceEqual(other.Directors ?? []) &&
               (Writers ?? []).SequenceEqual(other.Writers ?? []);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TConst, Directors, Writers);
    }

    public static bool operator ==(TitleCrew? left, TitleCrew? right)
    {
        return EqualityComparer<TitleCrew>.Default.Equals(left, right);
    }

    public static bool operator !=(TitleCrew? left, TitleCrew? right)
    {
        return !(left == right);
    }
}