using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace IMDB.API.ApiService;

public static class Extensions
{
    private static readonly SHA256 _hasher = SHA256.Create();

    public static long Hash(this string s) => BitConverter.ToInt64(_hasher.ComputeHash(Encoding.UTF8.GetBytes(s)));

    public static ulong ToNumId(this string s) => ulong.Parse(s[2..]);

    public static string ToTConst(this ulong v) => "tt" + v.ToString().PadLeft(7, '0');

    public static string ToNConst(this ulong v) => "nm" + v.ToString().PadLeft(7, '0');

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

    

    

    public static List<string> GetPrimaryKeyColumnNames<TEntity>(this DbContext context) where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity)) ?? throw new Exception("Entity type not found");
        var primaryKey = entityType.FindPrimaryKey() ?? throw new Exception("Primary key not found");
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        return [.. primaryKey.Properties.Select(property => property.GetColumnName(storeObject)!)];
    }

    
}
