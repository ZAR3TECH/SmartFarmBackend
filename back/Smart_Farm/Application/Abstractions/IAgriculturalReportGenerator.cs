using Smart_Farm.DTOS;

namespace Smart_Farm.Application.Abstractions;

public interface IAgriculturalReportGenerator
{
    Task<GroqReportDto> GenerateArabicReportAsync(DiagnoseResultDto diagnoseResult, CancellationToken cancellationToken);
}
