namespace LeafLINQWeb.Models;

// JSON data received response message
// GET /api/Devices
// GET /api/Devices/ {id}
public class PlantAPIDeviceModel
{
    public string deviceId { get; set; }   
    public string deviceType { get; set; }
    public string isInError { get; set; }
    public string associatedApid { get; set; }
    public string lastPing { get; set; }
    public string ipAddress { get; set; }
    public string macAddress { get; set; }
    public string uptimeInSeconds { get; set; }
    public string plantId { get; set; }
    private const int alignData = -26;
    private const int alignHeading = -18;

    public override string ToString()
    {
        
        return $"{nameof(PlantAPIDeviceModel.deviceId),alignHeading}: {deviceId,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.deviceType),alignHeading}: {deviceType,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.isInError),alignHeading}: {isInError,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.associatedApid),alignHeading}: {associatedApid,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.lastPing),alignHeading}: {lastPing,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.ipAddress),alignHeading}: {ipAddress,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.macAddress),alignHeading}: {macAddress,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.uptimeInSeconds),alignHeading}: {uptimeInSeconds,alignData}\n" +
               $"{nameof(PlantAPIDeviceModel.plantId),alignHeading}: {plantId,alignData}\n";
    }
}

// JSON send message for GET PUT  /api/Plants/{plantId}
//                           POST /api/Plants/
//                           
public class BodyResponsePlantDeviceModel
{
    public string plantID { get; set; }
    public string deviceID { get; set; }
    private const int alignData = -26;
    private const int alignHeading = -18;

    public override string ToString()
    {
        return $"{nameof(BodyResponsePlantDeviceModel.plantID),alignHeading}: {plantID,alignData}\n" +
               $"{nameof(BodyResponsePlantDeviceModel.deviceID),alignHeading}: {deviceID,alignData}\n";
    }

}