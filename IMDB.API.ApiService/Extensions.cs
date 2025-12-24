using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace IMDB.API.ApiService;

public static class Extensions
{
    public static bool TryDelete(this FileInfo fileInfo)
    {
        try
        {
            fileInfo.Delete();
            return true;
        }
        catch { }
        return false;
    }


    public static bool HasValue([NotNullWhen(true)] this string? str) => str != null && !string.IsNullOrWhiteSpace(str);

    public static List<string>? ToStringList(this string? str)
    {
        if (!str.HasValue())
            return null;

        var ret = new List<string>();

        try
        {
            ret.AddRange(str.Split(',', StringSplitOptions.RemoveEmptyEntries));
            ret.RemoveAll(_ => _ == "\\N");
        }
        catch { }

        return ret.Count > 0 ? ret : null;
    }

    public static bool HasItems([NotNullWhen(true)] this List<string>? items) =>
        items != null && items.Any(_ => _.HasValue());


    public static string GetTableName<TEntity>(this DbContext context) where TEntity : class
    {
        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity))!;
        return entityType.GetTableName()!;
    }

    public static ushort? TryGetUShort(this string? s)
    {
        if (ushort.TryParse(s, out ushort ret))
            return ret;
        return null;
    }

    public static int? TryGetInt(this string? s)
    {
        if (int.TryParse(s, out int val))
            return val;
        return null;
    }

    public static uint? TryGetUInt(this string? s)
    {
        if (uint.TryParse(s, out uint ret))
            return ret;
        return null;
    }
}
