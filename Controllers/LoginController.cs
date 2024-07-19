
using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace LeafLINQWeb.Controllers;

public class LoginController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private HttpClient Client => _clientFactory.CreateClient("auth");
    private HttpClient UserClient => _clientFactory.CreateClient("api");
    private HttpClient PlantClient => _clientFactory.CreateClient("api-plant-data");

    public LoginController(IHttpClientFactory clientFactory) => _clientFactory = clientFactory;


    public IActionResult Index()
    {
        return View();
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult> LoginForm(LoginModel login)
    {
        if (ModelState.IsValid)
        {
            // The actual/original error messages relating to each point in the process have been 
            // commented out and replaced with this user friendly message. To debug it comment
            // all lines with ModelState.AddModelError("Error", errMsg); and uncomment the original
            // error line message.
            string errMsg = "There was a problem loading your credentials. Please try again or contact your administrator";

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json");
                var response = await UserClient.PostAsync("api/login", content);
                var result = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);

                if (response.IsSuccessStatusCode)
                {
                    var contentPlantAPI = new StringContent(JsonConvert.SerializeObject(new AuthentificationLoginModel()), Encoding.UTF8, "application/json");
                    var responsePlantAPI = await PlantClient.PostAsync("api/Auth/login", contentPlantAPI);
                    

                    if (responsePlantAPI.IsSuccessStatusCode)
                    {
                        var resultPlantAPI = await responsePlantAPI.Content.ReadAsStringAsync();
                        var loginResponsePlantAPI = JsonConvert.DeserializeObject<PlantResponse>(resultPlantAPI);
                        
                        HttpContext.Session.SetString(SessionKeys.UserType, loginResponse.UserType);
                        HttpContext.Session.SetInt32(SessionKeys.UserID, loginResponse.Id);
                        HttpContext.Session.SetString(SessionKeys.UserTokenLocalAPI, loginResponse.AccessToken);
                        HttpContext.Session.SetString(SessionKeys.RefreshTokenLocalAPI, loginResponse.RefreshToken);
                        HttpContext.Session.SetString(SessionKeys.SessionID, loginResponse.SessionID);
                        HttpContext.Session.SetString(SessionKeys.UserTokenExternalAPI, loginResponsePlantAPI.jwtToken);

                        var user = await GetUsersDetails();
                        
                        HttpContext.Session.SetString(SessionKeys.UserFullName, user.FullName);
                        HttpContext.Session.SetString(SessionKeys.UserPicUrl, user.PicUrl);

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        //ModelState.AddModelError("Error", "Error in Plant API login");
                        ModelState.AddModelError("Error", errMsg);
                        return View("Index");
                    }
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    //ModelState.AddModelError("Error", "Error in Web API login: Bad request");
                    ModelState.AddModelError("Error", errMsg);
                    return View("Index");
                }
                else
                {
                    //ModelState.AddModelError("Error", "Error in Web API login: Unexpected status code");
                    ModelState.AddModelError("Error", errMsg);
                    return View("Index");
                }
            }
            catch (Exception)
            {
                //ModelState.AddModelError("Error", $"Error in Web API login: {ex.Message}");
                ModelState.AddModelError("Error", errMsg);
                return View("Index");
            }
        }

        //ModelState.AddModelError("Error", "Model state is not valid");
        ModelState.AddModelError("Error", "Incorrect authentification details");
        return View("Index");
    }

    //[HttpPatch]
    public ActionResult Logout()
    {
        var sessionId = HttpContext.Session.GetString(SessionKeys.SessionID);
        var response = UserClient.PatchAsync($"api/User/logout?sessionId={sessionId}", null).Result;
        if (response.IsSuccessStatusCode)
        {
            HttpContext.Session.Clear();
        }
        return RedirectToAction("Index", "Login");
    }

    [HttpGet]
    public async Task<UserModel> GetUsersDetails()
    {
        // Make the API request to retrieve users
        var response = await UserClient.GetAsync("api/User/user");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserModel>(result);

            return user;
        }
        else
        {
            return null;
        }
    }
}
