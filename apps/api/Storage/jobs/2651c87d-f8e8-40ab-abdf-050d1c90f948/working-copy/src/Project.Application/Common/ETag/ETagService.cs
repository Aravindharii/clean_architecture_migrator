using System.Text;
using Project.Application.Common.Exceptions;

namespace Project.Application.Common.ETag;

public interface IETagService
{
    string ComputeStrong(int id, DateTime updatedAt);

    string? ComputeForPayload(object? data);

    bool MatchesAny(string? requested, string computed, bool weak);

    void EnsurePreconditionMet(string? ifMatch, int id, DateTime updatedAt);
}

public class ETagService : IETagService
{
    public string ComputeStrong(int id, DateTime updatedAt)
        => $"\"{id}:{ToCanonicalTicks(updatedAt)}\"";

    public string? ComputeForPayload(object? data)
    {
        if (data is null)
            return null;

        if (TryGetPaged(data, out var items, out var page, out var size, out var total))
        {
            return ComputeItems(items, $"p{page}s{size}t{total};");
        }

        if (data is System.Collections.IEnumerable enumerable and not string)
        {
            return ComputeItems(enumerable.Cast<object?>().ToList(), string.Empty);
        }

        if (TryGetEntityStamp(data, out var id, out var updatedAt))
            return ComputeStrong(id, updatedAt);

        return null;
    }

    public bool MatchesAny(string? requested, string computed, bool weak)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return false;

        var requestedTags = requested.Trim();
        if (requestedTags == "*")
            return true;

        var computedInner = StripTag(computed, weak);

        foreach (var part in requestedTags.Split(','))
        {
            if (string.Equals(StripTag(part.Trim(), weak), computedInner, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public void EnsurePreconditionMet(string? ifMatch, int id, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return;

        if (!MatchesAny(ifMatch, ComputeStrong(id, updatedAt), weak: false))
        {
            throw new PreconditionFailedException(
                "The resource has been modified by another request. Reload it and try again.");
        }
    }

    private string? ComputeItems(IReadOnlyList<object?> items, string prefix)
    {
        var canonical = new StringBuilder(prefix);

        foreach (var item in items)
        {
            if (item is null || !TryGetEntityStamp(item, out var id, out var updatedAt))
                return null;

            canonical.Append(id).Append(':').Append(ToCanonicalTicks(updatedAt)).Append(';');
        }

        if (canonical.Length == 0)
            canonical.Append("empty");

        return $"\"{canonical}\"";
    }

    private static bool TryGetEntityStamp(object obj, out int id, out DateTime updatedAt)
    {
        id = default;
        updatedAt = default;

        var type = obj.GetType();
        var idProperty = type.GetProperty("Id");
        var updatedAtProperty = type.GetProperty("UpdatedAt");

        if (idProperty?.PropertyType != typeof(int) ||
            updatedAtProperty?.PropertyType != typeof(DateTime))
        {
            return false;
        }

        id = (int)idProperty.GetValue(obj)!;
        updatedAt = (DateTime)updatedAtProperty.GetValue(obj)!;
        return true;
    }

    private static bool TryGetPaged(object obj, out List<object?> items, out int page, out int size, out int total)
    {
        items = [];
        page = default;
        size = default;
        total = default;

        var type = obj.GetType();
        var itemsProperty = type.GetProperty("Items");
        var pageProperty = type.GetProperty("Page");
        var sizeProperty = type.GetProperty("Size");
        var totalProperty = type.GetProperty("TotalItems");

        if (itemsProperty is null || pageProperty is null || sizeProperty is null || totalProperty is null)
            return false;

        if (pageProperty.PropertyType != typeof(int) ||
            sizeProperty.PropertyType != typeof(int) ||
            totalProperty.PropertyType != typeof(int) ||
            itemsProperty.GetValue(obj) is not System.Collections.IEnumerable enumerable)
        {
            return false;
        }

        items = enumerable.Cast<object?>().ToList();
        page = (int)pageProperty.GetValue(obj)!;
        size = (int)sizeProperty.GetValue(obj)!;
        total = (int)totalProperty.GetValue(obj)!;
        return true;
    }

    private static long ToCanonicalTicks(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
        const long microsecondTicks = 10;
        return (utc.Ticks / microsecondTicks) * microsecondTicks;
    }

    private static string StripTag(string tag, bool weak)
    {
        var value = tag.Trim();

        if (weak && value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..];

        return value.Trim('"');
    }
}
