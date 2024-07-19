namespace LeafLINQWeb.Models
{
    public class DeviceAPISensorStatusRequest
    {
        public string DeviceId { get; set; }
        public string SensorType { get; set; }
        public bool IsInError { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
