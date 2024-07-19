using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using LeafLINQWeb.Models;

namespace LeafLINQWeb.Filters;

public class AuthorizeUserAttribute : Attribute, IAuthorizationFilter
{

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var customerID = context.HttpContext.Session.GetInt32(SessionKeys.UserID);
        if (!customerID.HasValue)
            context.Result = new RedirectToActionResult("Index", "Login", null);
    }
}
