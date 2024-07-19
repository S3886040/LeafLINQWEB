
namespace LeafLINQWeb.Models
{
    public class PlantAPISensorReadingModel
    {
        public int ReadingId { get; set; }
        public string DeviceId { get; set; }
        public double? Temperature { get; set; }
        public double? Humidity { get; set; }
        public double? SoilMoisture { get; set; }
        public double? LightIntensity { get; set; }
        public DateTime Timestamp { get; set; }
        public int? PlantID { get; set; }
        public virtual PlantAPIDeviceModel Device { get; set; }
        public virtual PlantModel Plant { get; set; }
    }
}
