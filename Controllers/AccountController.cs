using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace LeafLINQWeb.Controllers;


public class AccountController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private HttpClient Client => _clientFactory.CreateClient("auth");
    private HttpClient PlantClient => _clientFactory.CreateClient("api-plant-data");

    public AccountController(IHttpClientFactory clientFactory) => _clientFactory = clientFactory;

    [HttpGet]
    public ActionResult Login()
    {
        return View();
    }
    
    public ActionResult Logout()
    {
        var sessionId = HttpContext.Session.GetString(SessionKeys.SessionID);
        var response = Client.DeleteAsync($"api/User/logout?sessionId={sessionId}").Result;
        if(response.IsSuccessStatusCode) 
        {
            HttpContext.Session.Clear();
        }
        return RedirectToAction("Login", "Account");
    }
}