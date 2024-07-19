using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using LeafLINQWeb.Models;

namespace LeafLINQWeb.Filters;

/*This is a filter to add to all admin only level Controllers*/
public class AccessControlFilterAttribute : Attribute, IAuthorizationFilter
{

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var accessLevel = context.HttpContext.Session.GetString(SessionKeys.UserType);
        if (!accessLevel.Equals("A") && !accessLevel.Equals("S"))
            context.Result = new RedirectToActionResult("Index", "Home", null);
    }
}
