using System.Text;
using System.Text.Json;
using Smart_Farm.Application.Abstractions;
using Smart_Farm.Common;
using Smart_Farm.DTOS;

namespace Smart_Farm.Infrastructure.External;

public class GrogAgriculturalReportGenerator(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GrogAgriculturalReportGenerator> logger) : IAgriculturalReportGenerator
{
    public async Task<GroqReportDto> GenerateArabicReportAsync(DiagnoseResultDto diagnoseResult, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Groq:ApiKey is not configured.");

        var model    = configuration["Groq:Model"] ?? "meta-llama/llama-4-scout-17b-16e-instruct";
        var endpoint = "https://api.groq.com/openai/v1/chat/completions";

        var plantName = string.IsNullOrWhiteSpace(diagnoseResult.PlantName)
            ? "غير محدد"
            : diagnoseResult.PlantName.Trim();

        var apiResponseJson = ReportJsonSerializer.SerializeObject(new
        {
            disease    = diagnoseResult.Disease,
            confidence = diagnoseResult.Confidence,
            plant      = plantName
        });

        var prompt = BuildPrompt(apiResponseJson);

        var payload = JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        });

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Groq request failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Groq request failed: {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Groq returned an empty response.");

        // Strip markdown fences if model wraps the JSON in ```json ... ```
        var clean = text.Trim();
        if (clean.StartsWith("```"))
        {
            clean = clean.Split('\n', 2).Last();
            clean = clean[..clean.LastIndexOf("```")];
        }

        using var resultDoc = JsonDocument.Parse(clean.Trim());
        var root = resultDoc.RootElement;

        return new GroqReportDto
        {
            Disease    = root.GetProperty("disease").GetString()    ?? string.Empty,
            Symptoms   = root.GetProperty("symptoms").GetString()   ?? string.Empty,
            Causes     = root.GetProperty("causes").GetString()     ?? string.Empty,
            Treatment  = root.GetProperty("treatment").GetString()  ?? string.Empty,
            Prevention = root.GetProperty("prevention").GetString() ?? string.Empty
        };
    }

    private static string BuildPrompt(string apiResponseJson)
    {
        return $$"""
أنت خبير زراعي متخصص. حلل هذا الرد من PlantNet واستخدم اسم النبات في تقريرك:
{{apiResponseJson}}

تعليمات مهمة:
- ممنوع منعا باتا استخدام أي كلمة أو مصطلح إنجليزي، الرد بالكامل عربي
- لو بعتلك اسم النبات ف json اذكره فالرد بتاعك 

رد فقط بـ JSON صالح بهذا الشكل بالضبط بدون أي نص إضافي أو markdown أو مقدمة أو خاتمة:
{
  "disease": " بالكامل اسم المرض بالعربية",
  "symptoms": "وصف الأعراض التي تظهر على النبات",
  "causes": "لا تزيد عن 3 اسطر أسباب الإصابة بهذا المرض",
  "treatment": "لا تزيد عن 3 اسطر طرق العلاج الموصى بها",
  "prevention": "لا تزيد عن 3 اسطر طرق الوقاية من هذا المرض"
}
""";
    }
}
