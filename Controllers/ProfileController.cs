using LeafLINQWeb.Filters;
using LeafLINQWeb.Models;
using LeafLINQWeb.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace LeafLINQWeb.Controllers;

[AuthorizeUser]
public class ProfileController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private HttpClient Client => _clientFactory.CreateClient("api");
    private readonly BlobStorageService _blobStorageService;

    public ProfileController(IHttpClientFactory clientFactory, BlobStorageService blobStorageService)
    {
        _clientFactory = clientFactory;
        _blobStorageService = blobStorageService;
    }

    public async Task<IActionResult> Index()
    {
        // Make the API request to retrieve users
        var response = await Client.GetAsync("api/User/user");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<ProfileUpdateModel>(result);

            // Return the view with the list of users
            return View(user);
        }
        else
        {
            ModelState.AddModelError("Error", "Error Retireving List");
            return View();
        }
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> UpdateProfileAsync(ProfileUpdateModel userModel)
    {
        // Check Modelstate is valid
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("Error", "There were Errors in the form, Please Amend");
            return RedirectToAction("UpdateProfileWithErrors", userModel);
        }
        // If image file is defined, then new image was passed to method and requires uploading, otherwise 
        // old image url will be used.
        if (userModel.ImageFile != null && userModel.ImageFile.Length > 0)
        {
            string imageUrl = await _blobStorageService.UploadImageAsync(userModel.ImageFile, userModel.Id);
            userModel.PicUrl = imageUrl;
        }

        var content = new StringContent(JsonConvert.SerializeObject(userModel), Encoding.UTF8, "application/json");
        var response = Client.PutAsync("api/User/userUpdate", content).Result;

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "Home");
        }
        else
        {
            ModelState.AddModelError("Error", "Error Changing Info");
            return RedirectToAction("UpdateProfileWithErrors", userModel);
        }

    }

    // A alternative method used for edit user when errors are present
    public IActionResult UpdateProfileWithErrors(ProfileUpdateModel userModel)
    {
        return View("Index", userModel);
    }

}
