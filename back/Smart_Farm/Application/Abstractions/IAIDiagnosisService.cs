using Smart_Farm.DTOS;

namespace Smart_Farm.Application.Abstractions;

public interface IAIDiagnosisService
{
    // History — filtered by current user
    Task<IReadOnlyList<AIDiagnosisResponseDto>> GetAllAsync(int userId, CancellationToken cancellationToken);

    Task<AIDiagnosisResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // Single endpoint: runs PlantNet + grog + saves everything
    Task<DiagnoseFullResultDto> DiagnoseAsync(DiagnoseRequest request, int userId, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, UpdateAIDiagnosisRequestDto request, CancellationToken cancellationToken);

    Task<GroqReportDto?> RegenerateReportAsync(int id, int userId, CancellationToken cancellationToken);
}
