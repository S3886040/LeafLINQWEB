using System.Net.Http.Headers;
using LeafLINQWeb.Models;

namespace LeafLINQWeb.Handler;

public class PlantAPICustomHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PlantAPICustomHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string token = string.Empty;

        try
        {
            token = _httpContextAccessor.HttpContext.Session.GetString(SessionKeys.UserTokenExternalAPI);
        } catch (NullReferenceException)
        {
            // Scheduler cannot use session, alternative is a Singleton. 
            token = PseudoSession.Instance.BackgroundExternalAPIToken;
        }
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
