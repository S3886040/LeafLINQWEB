using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;

namespace LeafLINQWeb.Models;

public class PlantModel
{
    // Plant ID
    [Display(Name = "Plant Id")]
    public int Id { get; set; }

    // Plant Name
    [DataType(DataType.Text, ErrorMessage = "Non text based data encountered."), Required, Display(Name = "Name"), StringLength(40)]
    public string Name { get; set; }

    // Plant Description
    [DataType(DataType.Text, ErrorMessage = "Non text based data encountered."), Required, Display(Name = "Desc"), StringLength(200)]
    public string Desc { get; set; }

    // Plant Picture
    [Display(Name = "Plant Image"), Required, StringLength(200)]
    public string PicUrl { get; set; }

    // Plant Picture File
    [Display(Name = "Plant Image")]
    public IFormFile ImageFile { get; set; }

    // Plant Location
    [Display(Name = "Location"), Required, StringLength(10)]
    public string Location { get; set; }

    // Plant Level
    [Display(Name = "Level"), Required, StringLength(10)]
    public string Level { get; set; }

    // Plant Last time watered
    public DateTime LastWateredDate { get; set; } = DateTime.UtcNow;

    // Plant Health Status
    public HealthCheckStatus HealthCheckStatus { get; set; } = HealthCheckStatus.Healthy;

    // Plant Reports 
    public ReportsModel PlantReport { get; set; }
    // Plant Sensor data

    // -- Plants User
    public List<SelectListItem> UserSelectList { get; set; }
    public int UserId { get; set; }
    public virtual UserModel UserModel { get; set; }

    // -- Plants Device
    public List<SelectListItem> PlantDeviceList { get; set; }

    [Display(Name = "Device"), Required, StringLength(12)]
    public string DeviceId { get; set; }
    public virtual PlantModel DeviceModel { get; set; }

    // -- Plants Device Status
    public List<DeviceAPISensorStatusRequest> DeviceAPISensorStatusRequest { get; set; }


    public string PlantGroupdId { get; set; }
    public PlantGroupModel PlantGroupdModel { get; set; }
    private const int alignData = -26;
    private const int alignHeading = -18;
    public const decimal criticalThreshold = 25;
    public const decimal alertThreshold = 50;

    public override string ToString()
    {
        return $"{nameof(PlantModel.Id),alignHeading}: {Id,alignData}\n" +
               $"{nameof(PlantModel.Name),alignHeading}: {Name,alignData}\n" +
               $"{nameof(PlantModel.Desc),alignHeading}: {Desc,alignData}\n" +
               $"{nameof(PlantModel.Location),alignHeading}: {Location,alignData}\n" +
               $"{nameof(PlantModel.Level),alignHeading}: {Level,alignData}\n" +
               $"{nameof(PlantModel.LastWateredDate),alignHeading}: {LastWateredDate,alignData}\n" +
               $"{nameof(PlantModel.HealthCheckStatus),alignHeading}: {HealthCheckStatus,alignData}\n";
    }

    public bool UpdateHealthStatus(PlantSensorReadings plantSensed)
    {
        bool canParse = true;
        HealthCheckStatus initialCheck = HealthCheckStatus;

        if (!Decimal.TryParse(plantSensed.soilMoisture, out var moisture)) canParse = false;
        if(!Decimal.TryParse(plantSensed.humidity, out var humidity)) canParse = false;

        if (canParse)
        {
            if (Decimal.Parse(plantSensed.soilMoisture) < criticalThreshold)
            {
                initialCheck = HealthCheckStatus.Critical;
            }
            else if (Decimal.Parse(plantSensed.soilMoisture) < alertThreshold)
            {
                initialCheck = HealthCheckStatus.Alert;
            }
            else if (Decimal.Parse(plantSensed.humidity) < criticalThreshold)
            {
                initialCheck = HealthCheckStatus.Critical;
            }
            else if (Decimal.Parse(plantSensed.humidity) < alertThreshold)
            {
                initialCheck = HealthCheckStatus.Alert;
            }
            else
            {
                initialCheck = HealthCheckStatus.Healthy;
            }
        }

        if( initialCheck != HealthCheckStatus)
        {
            HealthCheckStatus = initialCheck;
            return true;
        }

        return false;
    }
}

public enum HealthCheckStatus
{
    Healthy,
    Alert,
    Inactive,
    Critical
}