using System.Text.Encodings.Web;
using System.Text.Json;
using Smart_Farm.DTOS;

namespace Smart_Farm.Common;

/// <summary>
/// JSON helpers for persisting Groq reports. Uses relaxed escaping so Arabic is stored as UTF-8 text, not \uXXXX sequences.
/// </summary>
public static class ReportJsonSerializer
{
    private static readonly JsonSerializerOptions StorageOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(GroqReportDto report) =>
        JsonSerializer.Serialize(report, StorageOptions);

    public static string SerializeObject(object value) =>
        JsonSerializer.Serialize(value, StorageOptions);

    public static GroqReportDto? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<GroqReportDto>(json, StorageOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
