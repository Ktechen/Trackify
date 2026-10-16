using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trackify.Application.TrackPlans;

/// <summary>
/// Reads and writes <see cref="TrackPlanDocument"/> as JSON. Enums go out as names and numbers stay
/// culture-invariant, so the file is readable and means the same thing on every machine.
/// </summary>
public static class TrackPlanSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serialises <paramref name="document"/> to indented JSON.</summary>
    public static string Write(TrackPlanDocument document) => JsonSerializer.Serialize(document, Options);

    /// <summary>
    /// Parses <paramref name="json"/>. Returns false with a human-readable <paramref name="error"/>
    /// rather than throwing, because the usual cause is a person handing over the wrong file.
    /// </summary>
    public static bool TryRead(string json, out TrackPlanDocument document, out string error)
    {
        document = new TrackPlanDocument();

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "The file is empty.";
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<TrackPlanDocument>(json, Options);

            if (parsed is null)
            {
                error = "The file does not contain a track plan.";
                return false;
            }

            document = parsed;
            error = "";
            return true;
        }
        catch (JsonException exception)
        {
            error = $"The file is not valid JSON: {exception.Message}";
            return false;
        }
    }
}
