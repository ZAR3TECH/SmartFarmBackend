namespace Smart_Farm.DTOS;

public class DiagnoseRequest
{
    public IFormFile? Image { get; set; }
    public int? Cid { get; set; }   // ← optional: diagnose with or without a crop
}
