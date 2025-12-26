using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

    

    public static List<string> GetColumnNames<TEntity>(this DbContext context) where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity)) ?? throw new Exception("Entity type not found");
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        return [.. entityType.GetProperties().Select(property => property.GetColumnName(storeObject)!)];
    }

    public static Type GetColumnType<TEntity>(this DbContext context, string name) where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity)) ?? throw new Exception("Entity type not found");
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var property = entityType.GetProperties().First(_ => _.GetColumnName(storeObject) == name);
        return property.ClrType;
    }

    public static List<string> GetPrimaryKeyColumnNames<TEntity>(this DbContext context) where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity)) ?? throw new Exception("Entity type not found");
        var primaryKey = entityType.FindPrimaryKey() ?? throw new Exception("Primary key not found");
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        return [.. primaryKey.Properties.Select(property => property.GetColumnName(storeObject)!)];
    }

    public static void AppendCSVField(this StringBuilder sb, object? value, bool addComma)
    {

        if (value != null)
        {
            var oType = value.GetType();
            if (oType == typeof(string))
            {
                var str = (string)value;
                if (str.HasValue())
                {
                    if (str.Contains(',') || str.Contains('"'))
                        str = "\"" + str.Replace("\"", "\"\"") + "\"";
                    sb.Append(str);
                }
            }
            else if (value is List<string> lst)
            {
                if (lst.HasItems())
                {
                    string str = "{" + string.Join(',', lst) + "}";
                    if (str.Contains(',') || str.Contains('"'))
                        str = "\"" + str.Replace("\"", "\"\"") + "\"";
                    sb.Append(str);
                }
            }
            else
            {
                sb.Append(value);
            }
        }

        if (addComma)
            sb.Append(',');
    }
}
