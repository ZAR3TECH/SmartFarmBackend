using Smart_Farm.Application.Abstractions;
using Smart_Farm.Common;
using Smart_Farm.DTOS;
using Smart_Farm.Models;

namespace Smart_Farm.Application.Services;

public class AIDiagnosisService(
    IAIDiagnosisRepository repository,
    IPlantDiseaseIdentifier plantDiseaseIdentifier,
    IAgriculturalReportGenerator reportGenerator,
    CloudinaryService cloudinaryService) : IAIDiagnosisService
{
    public async Task<IReadOnlyList<AIDiagnosisResponseDto>> GetAllAsync(int userId, CancellationToken cancellationToken)
        => await repository.GetAllAsync(userId, cancellationToken);

    public async Task<AIDiagnosisResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken);

    public async Task<DiagnoseFullResultDto> DiagnoseAsync(DiagnoseRequest request, int userId, CancellationToken cancellationToken)
    {
        if (request.Image is null || request.Image.Length == 0)
            throw new ArgumentException("No image uploaded", nameof(request));

        // ── 1. Read image bytes once ─────────────────────────────────────────
        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await request.Image.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }

        // ── 2. Run PlantNet ──────────────────────────────────────────────────
        await using var predictionStream = new MemoryStream(bytes);
        var prediction = await plantDiseaseIdentifier.IdentifyAsync(
            predictionStream,
            request.Image.FileName,
            request.Image.ContentType,
            cancellationToken);

        // ── 3. Find or create Disease row ────────────────────────────────────
        var disease = await repository.FindDiseaseByNameAsync(prediction.DiseaseName, cancellationToken);
        if (disease is null)
        {
            disease = new Disease { Name = prediction.DiseaseName };
            await repository.AddDiseaseAsync(disease, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        // ── 4. Resolve Cid → Pid → PLANT.Name (+ FarmId) ─────────────────────
        int? resolvedCid    = null;
        int? resolvedPid    = null;
        int? resolvedFarmId = null;
        string? plantName   = null;

        if (request.Cid.HasValue)
        {
            var cropInfo = await repository.GetCropPlantInfoByCidAsync(
                request.Cid.Value, userId, cancellationToken);
            if (cropInfo is not null)
            {
                resolvedCid    = cropInfo.Cid;
                resolvedPid    = cropInfo.Pid;
                resolvedFarmId = cropInfo.FarmId;
                plantName      = cropInfo.PlantName;
            }
        }

        // ── 5. Save diagnosis row to get ADid ────────────────────────────────
        var diagnosis = new AI_Diagnosis
        {
            DiagnosisDate      = DateTime.UtcNow,
            Result             = prediction.DiseaseName,
            Confidence = prediction.Confidence,  
            UserId = userId,
            Did                = disease.Did,
            Cid                = resolvedCid,
            Pid                = resolvedPid,
            FarmId             = resolvedFarmId,
            plant_image        = null,
            GrogArabicReport = null
        };

        await repository.AddAsync(diagnosis, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // ── 6. Upload image to Cloudinary ────────────────────────────────────
        string? imageUrl = null;
        try
        {
            await using var uploadStream = new MemoryStream(bytes);
            imageUrl = await cloudinaryService.UploadImageAsync(
                uploadStream,
                request.Image.FileName,
                folder: "smart_farm/diagnoses",
                publicId: $"diagnosis_{diagnosis.ADid}");

            diagnosis.plant_image = imageUrl;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AIDiagnosisService] Cloudinary upload failed for diagnosis {diagnosis.ADid}: {ex.Message}");
        }

        // ── 7. Call Groq → structured report ─────────────────────────────────
        GroqReportDto report;
        try
        {
            var plantNetResult = new DiagnoseResultDto
            {
                Id          = diagnosis.ADid,
                Disease     = prediction.DiseaseName,
                Confidence  = prediction.Confidence,
                PlantName   = plantName,
                Saved       = true,
                plant_image = imageUrl
            };

            report = await reportGenerator.GenerateArabicReportAsync(plantNetResult, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AIDiagnosisService] Groq report failed for diagnosis {diagnosis.ADid}: {ex.Message}");

            report = new GroqReportDto
            {
                Disease    = prediction.DiseaseName,
                Symptoms   = "لا توجد معلومات.",
                Causes     = "لا توجد معلومات.",
                Treatment  = "لا توجد معلومات.",
                Prevention = "لا توجد معلومات."
            };
        }

        // Store report as JSON string
        diagnosis.GrogArabicReport = ReportJsonSerializer.Serialize(report);

        // ── 8. Single final save ─────────────────────────────────────────────
        await repository.SaveChangesAsync(cancellationToken);

        return new DiagnoseFullResultDto
        {
            ADid          = diagnosis.ADid,
            DiagnosisDate = diagnosis.DiagnosisDate,
            Confidence    = prediction.Confidence,
            Did           = disease.Did,
            Cid           = resolvedCid,
            plant_image   = imageUrl,
            Report        = report
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateAIDiagnosisRequestDto request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindEntityByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        existing.DiagnosisDate = request.DiagnosisDate;
        existing.Result        = request.Result;
        existing.Did           = request.Did;
        existing.Cid           = request.Cid;

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GroqReportDto?> RegenerateReportAsync(
        int id, int userId, CancellationToken cancellationToken)
    {
        var entity = await repository.FindEntityByIdAsync(id, cancellationToken);
        if (entity is null || entity.UserId != userId)
            return null;

        var plantName = await ResolvePlantNameAsync(
            entity.Cid, entity.Pid, userId, cancellationToken);

        var plantNetResult = new DiagnoseResultDto
        {
            Id = entity.ADid,
            Disease = entity.Result,
            Confidence = entity.Confidence,
            PlantName = plantName,
            Saved = true,
            plant_image = entity.plant_image
        };

        var report = await reportGenerator.GenerateArabicReportAsync(plantNetResult, cancellationToken);
        entity.GrogArabicReport = ReportJsonSerializer.Serialize(report);
        await repository.SaveChangesAsync(cancellationToken);
        return report;
    }

    private async Task<string?> ResolvePlantNameAsync(
        int? cid, int? pid, int userId, CancellationToken cancellationToken)
    {
        if (cid.HasValue)
        {
            var cropInfo = await repository.GetCropPlantInfoByCidAsync(
                cid.Value, userId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cropInfo?.PlantName))
                return cropInfo.PlantName;
        }

        if (pid.HasValue)
            return await repository.GetPlantNameByPidAsync(pid.Value, cancellationToken);

        return null;
    }
}
