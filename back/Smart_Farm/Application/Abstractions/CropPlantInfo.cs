namespace Smart_Farm.Application.Abstractions;

/// <summary>Resolved from CROP (Cid) → PLANT (Pid) → Name.</summary>
public sealed record CropPlantInfo(int Cid, int? Pid, int? FarmId, string? PlantName);
