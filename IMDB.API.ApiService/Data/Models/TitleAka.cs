using Microsoft.EntityFrameworkCore;

namespace IMDB.API.ApiService.Data.Models;

[PrimaryKey(nameof(TitleId), nameof(Ordering), nameof(Title))]
public class TitleAka : IEquatable<TitleAka?>
{
    public required string TitleId { get; set; }

    public int Ordering { get; set; }

    public required string Title { get; set; }

    public string? Region { get; set; }

    public string? Language { get; set; }

    public List<string>? Types { get; set; }

    public List<string>? Attributes { get; set; }

    public bool IsOriginalTitle { get; set; }





    public string DictKey() => $"{TitleId}.{Ordering}.{Title}";


    public override bool Equals(object? obj)
    {
        return Equals(obj as TitleAka);
    }

    public bool Equals(TitleAka? other)
    {
        return other is not null &&
               TitleId == other.TitleId &&
               Ordering == other.Ordering &&
               Title == other.Title &&
               Region == other.Region &&
               Language == other.Language &&
               (Types ?? []).SequenceEqual(other.Types ?? []) &&
               (Attributes ?? []).SequenceEqual(other.Attributes ?? []) &&
               IsOriginalTitle == other.IsOriginalTitle;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TitleId, Ordering, Title, Region, Language, Types, Attributes, IsOriginalTitle);
    }

    public static bool operator ==(TitleAka? left, TitleAka? right)
    {
        return EqualityComparer<TitleAka>.Default.Equals(left, right);
    }

    public static bool operator !=(TitleAka? left, TitleAka? right)
    {
        return !(left == right);
    }
}
