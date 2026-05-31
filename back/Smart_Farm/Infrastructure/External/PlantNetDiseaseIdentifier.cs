using System.Net.Http.Headers;
using System.Text.Json;
using Smart_Farm.Application.Abstractions;
namespace Smart_Farm.Infrastructure.External;
public class PlantNetDiseaseIdentifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PlantNetDiseaseIdentifier> logger) : IPlantDiseaseIdentifier
{
    public async Task<PlantDiseasePredictionResult> IdentifyAsync(
        Stream imageStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["PlantNet:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("PlantNet:ApiKey is not configured.");

        using var httpContent = new MultipartFormDataContent();
        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "image/jpeg");
        httpContent.Add(fileContent, "images", fileName);

        var client = httpClientFactory.CreateClient("PlantNet");

        // Use disease endpoint with Arabic language and auto organ detection
        var url = $"v2/diseases/identify?api-key={Uri.EscapeDataString(apiKey)}&lang=ar&no-reject=false";

        var response = await client.PostAsync(url, httpContent, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("PlantNet API error {Status}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"PlantNet API error: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            throw new InvalidOperationException("No disease results returned from PlantNet.");

        var first = results[0];

        // description contains the human-readable disease name e.g. "Aphis sp."
        var diseaseName = first.TryGetProperty("description", out var descEl)
            ? descEl.GetString()
            : null;

        // EPPO code as fallback e.g. "APHISP"
        if (string.IsNullOrWhiteSpace(diseaseName)
            && first.TryGetProperty("name", out var nameEl))
            diseaseName = nameEl.GetString();

        if (string.IsNullOrWhiteSpace(diseaseName))
            throw new InvalidOperationException("Could not extract disease name from PlantNet response.");

        var confidence = first.TryGetProperty("score", out var scoreEl)
            ? scoreEl.GetDouble()
            : 0d;

        return new PlantDiseasePredictionResult
        {
            DiseaseName = diseaseName,
            Confidence = confidence,
            RawResponse = json
        };
    }
}