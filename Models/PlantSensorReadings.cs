using System.Net.Mail;

namespace LeafLINQWeb.Models;

public class PlantSensorReadings
{
    // JSON field names - same casing 
    public string readingId { get; set; }
    public string deviceId { get; set; }
    public string temperature { get; set; }
    public string humidity { get; set; }
    public string soilMoisture { get; set; }
    public string lightIntensity { get; set; }
    public string timestamp { get; set; }
    public string plantId { get; set; }
    private const int alignData = -26;
    private const int alignHeading = -18;

    public override string ToString()
    {
        return $"{nameof(PlantSensorReadings.readingId),alignHeading}: {readingId,alignData}\n" +
               $"{nameof(PlantSensorReadings.deviceId),alignHeading}: {deviceId,alignData}\n" +
               $"{nameof(PlantSensorReadings.temperature),alignHeading}: {temperature,alignData}\n" +
               $"{nameof(PlantSensorReadings.humidity),alignHeading}: {humidity,alignData}\n" +
               $"{nameof(PlantSensorReadings.soilMoisture),alignHeading}: {soilMoisture,alignData}\n" +
               $"{nameof(PlantSensorReadings.lightIntensity),alignHeading}: {lightIntensity,alignData}\n" +
               $"{nameof(PlantSensorReadings.timestamp),alignHeading}: {timestamp,alignData}\n" +
               $"{nameof(PlantSensorReadings.plantId),alignHeading}: {plantId,alignData}\n";
    }
}