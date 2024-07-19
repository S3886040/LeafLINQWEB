using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using LeafLINQWeb.Models;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
//using mailslurp.Model;

namespace LeafLINQWeb.Filters;

public class AuthorizeNewLoginAttribute : Attribute, IAuthorizationFilter
{
    HttpClient AuthorisedClient;
    private const string routePathBase = "recovery/";

    public AuthorizeNewLoginAttribute()
    {
        AuthorisedClient = new HttpClient();
        AuthorisedClient.BaseAddress = new Uri("https://leaflinqwebappapi.azurewebsites.net");
    }

    public async void OnAuthorization(AuthorizationFilterContext context)
    {
        // Security check prevent somebody from changing another persons password.
        var email = context.HttpContext.Session.GetString(nameof(LoginModel.Email));
  
        if (email == null || email == "")
        {
            context.Result = new RedirectToActionResult("Index", "Login", null);
        }
        else
        {
            UserModel user = await GetUserAsync(email);
            if (user == null)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }
        //AuthorisedClient?.Dispose();
    }

    public async Task<UserModel> GetUserAsync(String email)
    {
        if (email != null && email != "")
        {
            string getPath = $"{routePathBase}GetUserUsingEmail?email=" + email;
            var response = AuthorisedClient.GetAsync(getPath).Result;
            var result = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonConvert.DeserializeObject<UserModel>(result);

            if (response.IsSuccessStatusCode)
            {
                return loginResponse;
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return null;
            }
        }
        
        return null;
    }

    public void DM(string message)
    {
        Console.WriteLine($"\n{message}\n");
    }
}
