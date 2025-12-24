using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class TitleBasic : IEquatable<TitleBasic?>
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



    public override bool Equals(object? obj)
    {
        return Equals(obj as TitleBasic);
    }

    public bool Equals(TitleBasic? other)
    {
        return other is not null &&
               TConst == other.TConst &&
               TitleType == other.TitleType &&
               PrimaryTitle == other.PrimaryTitle &&
               OriginalTitle == other.OriginalTitle &&
               IsAdult == other.IsAdult &&
               StartYear == other.StartYear &&
               EndYear == other.EndYear &&
               RuntimeMinutes == other.RuntimeMinutes &&
               (Genres ?? []).SequenceEqual(other.Genres ?? []);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(TConst);
        hash.Add(TitleType);
        hash.Add(PrimaryTitle);
        hash.Add(OriginalTitle);
        hash.Add(IsAdult);
        hash.Add(StartYear);
        hash.Add(EndYear);
        hash.Add(RuntimeMinutes);
        hash.Add(Genres);
        return hash.ToHashCode();
    }

    public static bool operator ==(TitleBasic? left, TitleBasic? right)
    {
        return EqualityComparer<TitleBasic>.Default.Equals(left, right);
    }

    public static bool operator !=(TitleBasic? left, TitleBasic? right)
    {
        return !(left == right);
    }
}