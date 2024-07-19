using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using LeafLINQWeb.Filters;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using LeafLINQWeb.Utilities;

namespace LeafLINQWeb.Controllers;

[AuthorizeUser]
public class PlantsController : Controller
{

    private readonly IHttpClientFactory _clientFactory;
    private readonly BlobStorageService _blobStorageService;
    private HttpClient Client => _clientFactory.CreateClient("api");
    private HttpClient PlantClient => _clientFactory.CreateClient("api-plant-data");

    public PlantsController(IHttpClientFactory clientFactory, BlobStorageService blobStorageService)
    {
        _clientFactory = clientFactory;
        _blobStorageService = blobStorageService;
    }

    // --------------
    // Plants
    // --------------
    [HttpGet]
    public async Task<IActionResult> Index(string search = null, int page = 1)
    {
        // Check if admin, if admin get all plants. If regular user get only relevant plants
        HttpResponseMessage response;
        if (HttpContext.Session.GetString(SessionKeys.UserType).Equals("A") ||
            HttpContext.Session.GetString(SessionKeys.UserType).Equals("S"))
        {
            // Make the API request to retrieve all users plants
            response = await Client.GetAsync("api/Plants/allPlants");
        }
        else
        {
            // Make the API request to retrieve this users plants
            response = await Client.GetAsync("api/Plants/plants");
        }

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var plants = JsonConvert.DeserializeObject<List<PlantModel>>(result);

            // Filter plants based on search parameter
            if (!string.IsNullOrEmpty(search))
            {
                plants = plants.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Location.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Pagination
            int pageSize = 10;
            int itemsToSkip = (page - 1) * pageSize;
            var nextPage = plants.OrderBy(b => b.LastWateredDate).Skip(itemsToSkip).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalNumberOfItems = plants.Count();

            nextPage = await GetPlantHealthCheckStatus(nextPage);

            return View(nextPage);
        }
        else
        {
            ModelState.AddModelError("Error", "Error retrieving plant list");
            return View();
        }
    }


    public async Task<List<PlantModel>> GetPlantHealthCheckStatus(List<PlantModel> plants)
    {
        foreach (var plant in plants)
        {
            var plantSensorReadings = await GetPlantSensorReadings(plant.Id);

            if (plantSensorReadings != null)
            {
                var latestReading = plantSensorReadings.OrderByDescending(r => DateTime.Parse(r.timestamp)).FirstOrDefault();

                if (latestReading != null)
                {
                    plant.UpdateHealthStatus(latestReading);
                }

            }

        }
        return plants;
    }

    public async Task<List<PlantSensorReadings>> GetPlantSensorReadings(int plantId)
    {
        try
        {
            var response = await PlantClient.GetAsync($"/api/SensorReadings/plant/{plantId}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<PlantSensorReadings>>(result);
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    // --------------
    // Plant
    // --------------
    [HttpGet]
    public async Task<IActionResult> Plant(int id)
    {
        // Make the API request to retrieve users
        var response = await Client.GetAsync($"api/Plants/plant?id={id}");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var plant = JsonConvert.DeserializeObject<PlantModel>(result);

            // Build reports
            var reports = await BuildReports(id);

            if (reports != null)
            {
                plant.PlantReport = reports;
            }

            // Device Sensor 
            var sensorStatus = await GetSensorStatusAsync(id);

            if(sensorStatus != null)
            {
                plant.DeviceAPISensorStatusRequest = GetMostRecentSensorData(sensorStatus);
            }


            if (HttpContext.Session.GetString(SessionKeys.UserType).Equals("A") || 
                HttpContext.Session.GetString(SessionKeys.UserType).Equals("S"))
            {
                plant.UserSelectList = await GetUserSelectListAsync();
                plant.PlantDeviceList = await GetDeviceSelectListAsync();
            }


            return View(plant);
        }
        else
        {
            ModelState.AddModelError("Error", "Error Retireving plant List");
            return View();
        }
    }


    [HttpGet]
    public async Task<PlantAPISensorReadingAverage> GetDailyReports(int plantId)
    {
        // Send the GET request and await the response
        var response = await PlantClient.GetAsync($"/api/SensorReadings/plant/{plantId}");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            // Deserialize the result into a list of sensor reading models
            var allReports = JsonConvert.DeserializeObject<List<PlantAPISensorReadingModel>>(result);

            // Find the most recent report based on the Timestamp
            var mostRecentReport = allReports.OrderByDescending(x => x.Timestamp).FirstOrDefault();

            if (mostRecentReport != null)
            {
                // Convert timestamp to start and end of that day
                DateTime timestamp = mostRecentReport.Timestamp;

                // Get the start of the day (00:00:00)
                DateTime startOfDay = timestamp.Date;

                // Get the end of the day (23:59:59)
                DateTime endOfDay = timestamp.Date.AddDays(1).AddSeconds(-1);

                // Format the startOfMonth and endOfMonth using the ISO 8601 format
                string formattedStartTime = startOfDay.ToString("yyyy-MM-ddTHH:mm:ss");
                string formattedEndTime = endOfDay.ToString("yyyy-MM-ddTHH:mm:ss");

                var dailyReport = await GetAverageSensorReadingsByPlant(plantId, formattedStartTime, formattedEndTime);

                return dailyReport;

            }
            else
            {
                // Handle the case where there are no reports
                return null;
            }
        }
        else
        {
            // Handle the case where the response is not successful
            return null;
        }
    }

    [HttpGet]
    public async Task<List<PlantAPISensorReadingDailyAverage>> GetMonthlyReports(int plantId)
    {
        var dailyAverages = await GetDailyAverageSensorReadingsByPlant(plantId, 30);

        return dailyAverages;
    }

    public async Task<ReportsModel> BuildReports(int id)
    {
        var reports = new ReportsModel
        {
            DailyReports = await GetDailyReports(id),
            MonthlyReports = await GetMonthlyReports(id)
        };
        return reports;
    }

    public async Task<List<SelectListItem>> GetDeviceSelectListAsync()
    {

        List<SelectListItem> deviceList = new List<SelectListItem>();
        // Make the API request to retrieve all plants
        var response = await PlantClient.GetAsync($"api/Devices/UnassignedPlantNodes");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var devices = JsonConvert.DeserializeObject<List<PlantAPIDeviceModel>>(result);

            foreach (var device in devices)
            {

                SelectListItem selectListItem = new SelectListItem()
                {
                    Value = device.deviceId.ToString(),
                    Text = device.deviceId.ToString(),
                };
                deviceList.Add(selectListItem);

            }

        }

        return deviceList;
    }

    // Average readings report with {plant id, start time, end time}
    [HttpGet]
    public async Task<List<PlantAPISensorReadingDailyAverage>> GetDailyAverageSensorReadingsByPlant(int plantId, int numberOfDays)
    {

        // Construct the query string with the formatted date-time parameters
        var queryString = $"api/Reporting/sensorreadings/dailyaverage/plant?plantId={plantId}&numberOfDays={numberOfDays}";

        // Send the GET request with the query string
        var response = await PlantClient.GetAsync(queryString);

        if (response.IsSuccessStatusCode)
        {
            var results = await response.Content.ReadAsStringAsync();
            var report = JsonConvert.DeserializeObject<List<PlantAPISensorReadingDailyAverage>>(results);
            return report;
        }
        else
        {
            return null;
        }
    }


    // Average readings report with {plant id, start time, end time}
    [HttpGet]
    public async Task<PlantAPISensorReadingAverage> GetAverageSensorReadingsByPlant(int plantId, string startTime, string endTime)
    {

        // Construct the query string with the formatted date-time parameters
        var queryString = $"/api/Reporting/sensorreadings/average/plant?plantId={plantId}&startTime={startTime}&endTime={endTime}";

        // Send the GET request with the query string
        var response = await PlantClient.GetAsync(queryString);

        if (response.IsSuccessStatusCode)
        {
            var results = await response.Content.ReadAsStringAsync();
            var report = JsonConvert.DeserializeObject<PlantAPISensorReadingAverage>(results);
            return report;
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public async Task<List<DeviceAPISensorStatusRequest>> GetSensorStatusAsync(int plantId)
    {

        // Construct the query string with the formatted date-time parameters
        var queryString = $"/api/SensorStatus/{plantId}";

        // Send the GET request with the query string
        var response = await PlantClient.GetAsync(queryString);

        if (response.IsSuccessStatusCode)
        {
            var results = await response.Content.ReadAsStringAsync();
            var report = JsonConvert.DeserializeObject<List<DeviceAPISensorStatusRequest>>(results);
            return report;
        }
        else
        {
            return null;
        }
    }

    public List<DeviceAPISensorStatusRequest> GetMostRecentSensorData(List<DeviceAPISensorStatusRequest> sensorData)
    {
        var recentSensorData = new List<DeviceAPISensorStatusRequest>();

        // Filter sensor data by sensor type and select the most recent one for each type
        foreach (var sensorType in new[] { "temp", "light", "moisture" })
        {
            var recentData = sensorData
                .Where(data => data.SensorType == sensorType)
                .OrderByDescending(data => data.Timestamp)
                .FirstOrDefault();

            if (recentData != null)
            {
                recentSensorData.Add(recentData);
            }
        }

        return recentSensorData;
    }

    // Get Sensor Readings with {plant id}
    public async Task<List<PlantAPISensorReadingModel>> GetSensorReading(int id)
    {
        // Construct the URL with the sensor reading ID
        var url = $"api/SensorReadings/{id}";

        // Assuming _httpClient is an instance of HttpClient
        var response = await PlantClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();

            // Check if the JSON starts with an array bracket
            if (json.TrimStart().StartsWith("["))
            {
                // Deserialize as a list
                var sensorReadings = JsonConvert.DeserializeObject<List<PlantAPISensorReadingModel>>(json);
                return sensorReadings;
            }
            else
            {
                // Deserialize as a single object and return it as a list with one item
                var singleSensorReading = JsonConvert.DeserializeObject<PlantAPISensorReadingModel>(json);
                return new List<PlantAPISensorReadingModel> { singleSensorReading };
            }
        }
        else
        {
            // Handle the error appropriately
            // This could involve throwing an exception, logging the error, etc.
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");
        }
    }


    // --------------
    // Add Plant
    // --------------
    [HttpGet]
    public async Task<IActionResult> AddPlant(PlantModel plant)
    {
		//Get Device data
        plant.PlantDeviceList = await GetDeviceSelectListAsync();
        ViewBag.Error = TempData["Error"];
        return View(plant);

    }

    [HttpPost]
    public async Task<IActionResult> AddNewPlant(PlantModel plant)
    {

        var userId = HttpContext.Session.GetInt32(nameof(UserModel.Id));
        plant.UserId = (int)userId;

        // If image file is defined, then new image was passed to method and requires uploading, otherwise 
        // old image url will be used.
        if (plant.ImageFile != null && plant.ImageFile.Length > 0)
        {
            string imageUrl = await _blobStorageService.UploadImageAsync(plant.ImageFile, plant.Id);
            plant.PicUrl = imageUrl;
        }

        var content = new StringContent(JsonConvert.SerializeObject(plant), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("api/plants/AddPlant", content);

        if (response.IsSuccessStatusCode)
        {
            // Handle successful response
            return RedirectToAction("Index", "Plants");
        }
        else
        {
            TempData["Error"] = "Error occurred while adding plant. Please try again later.";
            return RedirectToAction("AddPlant", "Plants");
        }
        // Return the AddPlant view with the model data
        return View(plant);
    }

 


    // --------------
    // Update Plant
    // --------------
    [HttpPost]
    public async Task<IActionResult> UpdatePlant(PlantModel updatedPlant)
    {
        // Get existing plant
        var plantId = updatedPlant.Id;
        PlantModel existingPlant = await GetSpecifiedPlant(plantId);

        // Update only the properties that have changed
        if (updatedPlant.Name != existingPlant.Name)
        {
            existingPlant.Name = updatedPlant.Name;
        }

        if (updatedPlant.Desc != existingPlant.Desc)
        {
            existingPlant.Desc = updatedPlant.Desc;
        }

        if (updatedPlant.Location != existingPlant.Location)
        {
            existingPlant.Location = updatedPlant.Location;
        }

        if (updatedPlant.UserId != existingPlant.UserId)
        {
            existingPlant.UserId = updatedPlant.UserId;
        }

        if (updatedPlant.Level != existingPlant.Level)
        {
            existingPlant.Level = updatedPlant.Level;
        }

        if (updatedPlant.DeviceId != existingPlant.DeviceId)
        {
            existingPlant.DeviceId = updatedPlant.DeviceId;
        }


        var addPlantToPlant = await AddPlantAPIPlant(plantId, updatedPlant.DeviceId);
        if (!addPlantToPlant)
        {
            ModelState.AddModelError("Error", "Plant id not working");
            return RedirectToAction("Plant", "Plants", existingPlant);
        }

        // Serialize the updated plant
        var updatedPlantJson = JsonConvert.SerializeObject(existingPlant);
        var content = new StringContent(updatedPlantJson, Encoding.UTF8, "application/json");

        // Send the updated plant data to the API
        var response = await Client.PutAsync($"api/plants/updatePlant", content);

        if (response.IsSuccessStatusCode)
        {
            // Set ViewBag with success message
            ViewBag.SuccessMessage = "Plant updated successfully.";
            return RedirectToAction("Plant", "Plants", updatedPlant);
        }
        else
        {
            ModelState.AddModelError("Error", "Error occurred while updating plant. Please try again later.");
            return RedirectToAction("Plant", "Plants", existingPlant);
        }

    }

    // Get a Specified Plant
    [HttpGet]
    public async Task<PlantModel> GetSpecifiedPlant(int plantId)
    {
        // Retrieve the existing plant from the API
        var getPlant = await Client.GetAsync($"api/Plants/plant?id={plantId}");

        if (!getPlant.IsSuccessStatusCode)
        {
            // If the plant is not found, return null
            return null;
        }

        // Deserialize the existing plant data
        var existingPlantJson = await getPlant.Content.ReadAsStringAsync();
        var existingPlant = JsonConvert.DeserializeObject<PlantModel>(existingPlantJson);

        return existingPlant;
    }

    // Add Plant to API plant
    [HttpPost]
    private Task<bool> AddPlantAPIPlant(int plantID, string deviceID)
    {
        // Build Plant Device Model
        PlantAPIPlantModel plantAPIPlantModel = new PlantAPIPlantModel
        {
            PlantID = plantID,
            DeviceID = deviceID
        };

        // Check if the request was successful
        var content = new StringContent(JsonConvert.SerializeObject(plantAPIPlantModel), Encoding.UTF8, "application/json");
        var response = PlantClient.PostAsync("api/Plants", content).Result;

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            return Task.FromResult(true);
        }
        else
        {
            return Task.FromResult(false);
        }

    }


    public async Task<List<SelectListItem>> GetUserSelectListAsync()
    {

        List<SelectListItem> userList = new List<SelectListItem>();
        // Make the API request to retrieve all plants
        var response = await Client.GetAsync($"api/admin/userListAll");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<UserModel>>(result);

            foreach (var user in users)
            {

                SelectListItem selectListItem = new SelectListItem()
                {
                    Value = user.Id.ToString(),
                    Text = $"{user.Id,-4} : {user.FullName}",
                };
                userList.Add(selectListItem);

            }

        }

        return userList;
    }


}
