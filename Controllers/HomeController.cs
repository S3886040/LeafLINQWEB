//using Azure.Core;
using LeafLINQWeb.Filters;
using LeafLINQWeb.Handler;
using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;

namespace LeafLINQWeb.Controllers;

[AuthorizeUser]
public class HomeController : Controller
{

    private readonly ILogger<HomeController> _logger;

    private readonly IHttpClientFactory _clientFactory;
    private HttpClient Client => _clientFactory.CreateClient("api");
    private HttpClient PlantClient => _clientFactory.CreateClient("api-plant-data");

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
    }

    // --------------
    // Dashboard
    // --------------
    [HttpGet]
    public async Task<IActionResult> Index(string search = null, int page = 1)
    {
        // Check for user id
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserID);

        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        // Check if admin, if admin get all plants. If regular user get only relevant plants
        HttpResponseMessage response;
        if (HttpContext.Session.GetString(SessionKeys.UserType).Equals("A") || 
            HttpContext.Session.GetString(SessionKeys.UserType).Equals("S"))
        {
            // Make the API request to retrieve users
            response = await Client.GetAsync("api/Plants/allPlants");
        }
        else
        {
            // Make the API request to retrieve users
            response = await Client.GetAsync("api/Plants/plants");
        }

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            List<PlantModel> plants = JsonConvert.DeserializeObject<List<PlantModel>>(result);

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

            if(plantSensorReadings != null )
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





    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
