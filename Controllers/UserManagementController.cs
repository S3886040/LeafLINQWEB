using LeafLINQWeb.Filters;
using LeafLINQWeb.Models;
using LeafLINQWeb.Models.UserManagement;
using LeafLINQWeb.Utilities;
using LeafLINQWeb.Views.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using System.Text;

namespace LeafLINQWeb.Controllers;


[AuthorizeUser]
[AccessControlFilter]
public class UserManagementController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private HttpClient Client => _clientFactory.CreateClient("api");
    private readonly BlobStorageService _blobStorageService;

    public UserManagementController(IHttpClientFactory clientFactory, BlobStorageService blobStorageService)
    {
        _clientFactory = clientFactory;
        _blobStorageService = blobStorageService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        // Make the API request to retrieve users
        var response = await Client.GetAsync("api/Admin/userList");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<UserModel>>(result);

            // Pagination
            int pageSize = 10;
            int itemsToSkip = (page - 1) * pageSize;
            var nextPage = users.OrderBy(b => b.Id).Skip(itemsToSkip).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalNumberOfItems = users.Count();

            // Return the view with the list of users
            return View(nextPage);
        }
        else
        {
            ModelState.AddModelError("Error", "Error Retireving List");
            return View();
        }
    }

    public async Task<IActionResult> ProfileViewAsync(int userId, int page = 1, string search = null)
    {
        // Make the API request to retrieve users
        var response = await Client.GetAsync($"api/Admin/user?userId={@userId}");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var userAndPlants = JsonConvert.DeserializeObject<AdminUserModel>(result);

            // Get account select list items
            /*            userAndPlants.AllPlants = await GetAccountSelectListAsync(userAndPlants.User.Id);*/
            if (!string.IsNullOrEmpty(search))
            {
                userAndPlants.Plants = userAndPlants.Plants.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Location.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Pagination
            int pageSize = 10;
            int itemsToSkip = (page - 1) * pageSize;
            var nextPage = userAndPlants.Plants.OrderBy(b => b.LastWateredDate).Skip(itemsToSkip).Take(pageSize).ToList();
            userAndPlants.Plants = nextPage;

            ViewBag.CurrentPage = page;
            ViewBag.TotalNumberOfItems = userAndPlants.Plants.Count();

            // Return the view with the list of users
            return View(userAndPlants);
        }
        else
        {
            ModelState.AddModelError("Error", "Error Retireving List");
            return View();
        }
    }

    public async Task<IActionResult> EditUser(int userId)
    {
        // Make the API request to retrieve users
        var response = await Client.GetAsync($"api/Admin/userOnly?userId={@userId}");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserModelWithFile>(result);
            return View(user);
        }
        else
        {
            ModelState.AddModelError("Error", "Error Retireving Info");
            return View();
        }
    }

    // A alternative method used for edit user when errors are present
    public IActionResult EditUserWithErrors(UserModelWithFile userModel)
    {
        ModelState.AddModelError("Error", "There were Errors in the form, Please Amend");
        return View("EditUser", userModel);
    }


    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> UpdateProfileAsync(UserModelWithFile userModel)
    {

        // Check Modelstate is valid
        if (!ModelState.IsValid)
        {
            return RedirectToAction("EditUserWithErrors", userModel);
        }
        // If image file is defined, then new image was passed to method and requires uploading, otherwise 
        // old image url will be used.
        if (userModel.ImageFile != null && userModel.ImageFile.Length > 0)
        {
            string imageUrl = await _blobStorageService.UploadImageAsync(userModel.ImageFile, userModel.Id);
            userModel.PicUrl = imageUrl;
        }

        var content = new StringContent(JsonConvert.SerializeObject(userModel), Encoding.UTF8, "application/json");
        var response = Client.PutAsync("api/Admin/adminUserUpdate", content).Result;

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("ProfileView", "UserManagement", new { userId = userModel.Id });
        }
        else
        {
            ModelState.AddModelError("Error", "Error Changing Info");
            return RedirectToAction("EditUser", "UserManagement", new { userId = userModel.Id });
        }

    }


    public IActionResult AddUser()
    {
        var userModel = new UserModelWithFile();
        return View(userModel);
    }

    // A alternative method used for edit user when errors are present
    public IActionResult AddUserWithErrors(UserModelWithFile userModel)
    {
        return View("AddUser", userModel);
    }


    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> AddUserProfileAsync(UserModelWithFile userModel)
    {
        
        // Check Modelstate is valid
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("Error", "There were Errors in the form, Please Amend");
            return View("AddUser", userModel);
        }
        // Serialize and send object to the api
        var content = new StringContent(JsonConvert.SerializeObject(userModel), Encoding.UTF8, "application/json");
        var response = Client.PutAsync("api/Admin/addUser", content).Result;
        var result = await response.Content.ReadAsStringAsync();

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // desirialize new user id
            var id = JsonConvert.DeserializeObject<int>(result);
            // set up object to update pic url in db
            var updatePicURL = new UpdatePicURLModel();
            updatePicURL.Id = id;

            if (userModel.ImageFile != null && userModel.ImageFile.Length > 0)
            {
                string imageUrl = await _blobStorageService.UploadImageAsync(userModel.ImageFile, id);
                updatePicURL.PicUrl = imageUrl;

                var urlContent = new StringContent(JsonConvert.SerializeObject(updatePicURL), Encoding.UTF8, "application/json");
                var urlResponse = Client.PutAsync("api/Admin/updatePicUrl", urlContent).Result;
            } 
            return RedirectToAction("ProfileView", "UserManagement", new { userId = id });
        }
        else if((int)response.StatusCode == 409) { }
        {
            ModelState.AddModelError("Error", "There were Errors in the form, Please Amend");
            ModelState.AddModelError("Email", "Email is already in the database");
            return View("AddUser", userModel);
        }

    }

    public async Task<IActionResult> DeleteUserAsync(int userId)
    {
        var response = await Client.DeleteAsync($"api/Admin/removeUser?userId={userId}");
        if(response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "UserManagement");
        }
        else
        {
            ModelState.AddModelError("Error", "Error Changing Info");
            return RedirectToAction("Index", "UserManagement");
        }

    }

    public async Task<IActionResult> RemoveUserAsync(int userId)
    {
        // Make the API request to retrieve users
        var response = await Client.GetAsync($"api/Admin/user?userId={@userId}");

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            var result = await response.Content.ReadAsStringAsync();
            var userAndPlants = JsonConvert.DeserializeObject<AdminUserModel>(result);

            // Return the view with the list of users
            return View(userAndPlants);
        }
        else
        {
            ModelState.AddModelError("Error", "Error Retireving List");
            return View();
        }
    }

    public async Task<IActionResult> BlockUserAsync(int userId)
    {
        // Make the API request to retrieve users
        var response = await Client.PatchAsync($"/api/Admin/blockUser?userId={@userId}", null);

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "UserManagement");
        }
        else
        {
            ModelState.AddModelError("Error", "Error While Blocking User");
            return RedirectToAction("Index", "UserManagement");
        }
    }


    public async Task<IActionResult> UnBlockUserAsync(int userId)
    {
        // Make the API request to retrieve users
        var response = await Client.PatchAsync($"api/Admin/unBlockUser?userId={@userId}", null);

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "UserManagement");
        }
        else
        {
            ModelState.AddModelError("Error", "Error While UnBlocking User");
            return View("Index");
        }
    }

}
