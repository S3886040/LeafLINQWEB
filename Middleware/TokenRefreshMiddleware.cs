using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Text;

namespace LeafLINQWeb.Middleware;

public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IHttpClientFactory httpClientFactory)
    {
        // set up variables for token refresh
        var session = context.Session;
        var refreshModel = new RefreshTokenModel();
        var Client = httpClientFactory.CreateClient("api");
        var PlantClient = httpClientFactory.CreateClient("api-plant-data");

        // set up model for api call
        refreshModel.AccessToken = session.GetString(SessionKeys.UserTokenLocalAPI);
        refreshModel.RefreshToken = session.GetString(SessionKeys.RefreshTokenLocalAPI);
        refreshModel.SessionId = session.GetString(SessionKeys.SessionID);

        // Make the api call, if a 204 no cotent comes back. No action taken. Which is most of the time.
        // If a 400 bad request comes back then we clear the session and log the user out as they have an expired refresh token.
        var content = new StringContent(JsonConvert.SerializeObject(refreshModel), Encoding.UTF8, "application/json");
        var response = Client.PostAsync("/api/refreshToken", content).Result;
        int statusCodeValue = (int)response.StatusCode;
        if (statusCodeValue == 200)
        {
            var contentTokens = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<RefreshTokenModel>(contentTokens);
            session.SetString(SessionKeys.UserTokenLocalAPI, tokenResponse.AccessToken);
            session.SetString(SessionKeys.RefreshTokenLocalAPI, tokenResponse.RefreshToken);

            var contentPlantAPI = new StringContent(JsonConvert.SerializeObject(new AuthentificationLoginModel()), Encoding.UTF8, "application/json");
            var responsePlantAPI = await PlantClient.PostAsync("api/Auth/login", contentPlantAPI);

            if (responsePlantAPI.IsSuccessStatusCode)
            {
                var resultPlantAPI = await responsePlantAPI.Content.ReadAsStringAsync();
                var loginResponsePlantAPI = JsonConvert.DeserializeObject<PlantResponse>(resultPlantAPI);
                session.SetString(SessionKeys.UserTokenExternalAPI, loginResponsePlantAPI.jwtToken);
            }
        }
        if (statusCodeValue == 400)
        {
            _ = Client.PatchAsync($"api/User/logout?sessionId={session.GetString(SessionKeys.SessionID)}", null).Result;

            session.Clear();

            context.Response.Redirect("/Login");

        }
        await _next(context);

    }

}

public static class TokenRefreshMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenRefreshMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenRefreshMiddleware>();
    }
}

