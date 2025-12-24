using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleRating : IEquatable<TitleRating?>
{
    [Key]
    public required string TConst { get; set; }

    public float AverageWeighting { get; set; }

    public int NumVotes { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TitleRating);
    }

    public bool Equals(TitleRating? other)
    {
        return other is not null &&
               TConst == other.TConst &&
               AverageWeighting == other.AverageWeighting &&
               NumVotes == other.NumVotes;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TConst, AverageWeighting, NumVotes);
    }

    public static bool operator ==(TitleRating? left, TitleRating? right)
    {
        return EqualityComparer<TitleRating>.Default.Equals(left, right);
    }

    public static bool operator !=(TitleRating? left, TitleRating? right)
    {
        return !(left == right);
    }
}