using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleEpisode : IEquatable<TitleEpisode?>
{
    [Key]
    public required string TConst { get; set; }

    public required string ParentTConst { get; set; }

    public int? SeasonNumber { get; set; }

    public int? EpisodeNumber { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TitleEpisode);
    }

    public bool Equals(TitleEpisode? other)
    {
        return other is not null &&
               TConst == other.TConst &&
               ParentTConst == other.ParentTConst &&
               SeasonNumber == other.SeasonNumber &&
               EpisodeNumber == other.EpisodeNumber;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TConst, ParentTConst, SeasonNumber, EpisodeNumber);
    }

    public static bool operator ==(TitleEpisode? left, TitleEpisode? right)
    {
        return EqualityComparer<TitleEpisode>.Default.Equals(left, right);
    }

    public static bool operator !=(TitleEpisode? left, TitleEpisode? right)
    {
        return !(left == right);
    }
}