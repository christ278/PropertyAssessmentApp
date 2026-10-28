using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

/// <summary>
/// Tolerant (de)serialization for <see cref="AssessmentPartitionItem"/> JSON stored in
/// GeneratedPartitionsJson or posted from the browser. Handles legacy/loosely-typed values such as
/// "classification":0 (number for a string property) and "" for dates/numbers, which otherwise make
/// System.Text.Json throw and silently drop partition data.
/// </summary>
public static class PartitionJsonHelper
{
    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly HashSet<string> _stringProps = typeof(AssessmentPartitionItem).GetProperties()
        .Where(p => p.PropertyType == typeof(string))
        .Select(p => p.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static JsonNode? Normalize(JsonNode? node)
    {
        if (node is JsonArray arr)
        {
            foreach (var el in arr) Normalize(el);
            return node;
        }
        if (node is not JsonObject obj) return node;

        foreach (var kv in obj.ToList())
        {
            if (kv.Value is not JsonValue v) continue;
            var kind = v.GetValueKind();
            if (kind == JsonValueKind.String && string.IsNullOrEmpty(v.GetValue<string>()))
            {
                obj.Remove(kv.Key);
            }
            else if (_stringProps.Contains(kv.Key) &&
                     (kind == JsonValueKind.Number || kind == JsonValueKind.True || kind == JsonValueKind.False))
            {
                obj[kv.Key] = v.ToJsonString();
            }
        }
        return node;
    }

    public static List<AssessmentPartitionItem> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<AssessmentPartitionItem>();
        var node = Normalize(JsonNode.Parse(json));
        return node.Deserialize<List<AssessmentPartitionItem>>(_readOptions) ?? new List<AssessmentPartitionItem>();
    }

    public static AssessmentPartitionItem? ParseItem(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var node = Normalize(JsonNode.Parse(json));
        return node.Deserialize<AssessmentPartitionItem>(_readOptions);
    }
}
