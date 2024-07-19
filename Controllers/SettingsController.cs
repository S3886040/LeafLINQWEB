using LeafLINQWeb.Filters;
using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeafLINQWeb.Controllers;

[AuthorizeUser]
public class SettingsController : Controller
    {
	public SettingsModel settings = new SettingsModel
	{
		SupportInformation = "For support, please contact support@example.com.",
		TemperatureUnit = 'C',
		EnableNotifications = true
	};

	public IActionResult Index()
        {
            return View(settings);
        }

    }
