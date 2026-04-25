namespace Smart_Farm.DTOS
{

    public class PlantRequestDto
    {
        public string? PhotoUrl { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Seed_type { get; set; }
        public string? Fertilizer_need { get; set; }
        public int? Days_to_harvest { get; set; }
        public string? Season { get; set; }
        public string? Humidity_range { get; set; }
        public string? Water_need { get; set; }
        public string? Soil_type { get; set; }
        public string? Temperature_range { get; set; }
    }
    public class PlantResponseDto
    {
        public int Pid { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Seed_type { get; set; }
        public string? Fertilizer_need { get; set; }
        public int? Days_to_harvest { get; set; }
        public string? Season { get; set; }
        public string? Humidity_range { get; set; }
        public string? Water_need { get; set; }
        public string? Soil_type { get; set; }
        public string? Temperature_range { get; set; }
    }
}