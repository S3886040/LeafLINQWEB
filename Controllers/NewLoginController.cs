using DotLiquid;
using LeafLINQWeb.Filters;
using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace LeafLINQWeb.Controllers;

// Handles a new login or forgotten password send email verification code to verify email account
// and user authentification.
public class NewLoginController : Controller
{

    private readonly IHttpClientFactory _clientFactory;
    private HttpClient Client => _clientFactory.CreateClient("auth");
    private HttpClient AuthorisedClient => _clientFactory.CreateClient("api");
    private IConfiguration _config;
    private const string routePathBase = "recovery/";
    private const string mediaType = "application/json";
    

    public NewLoginController(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config; 
    }
          
    [HttpPost]
    //[AuthorizeNewLogin]
    // Update new password to the User table.
    public async Task<ActionResult> NewLogin(LoginModel login)
    {
        // Exit straight away. First time into the page. No need to go any further.
        if ((login.Password == null || login.Password == "")
            && (login.ConfirmPassword == null || login.ConfirmPassword == "")) return View();

        if (ModelState.IsValid)
        {
            UserModel userModel = await GetUserAsync(login.Email);

            // New password and confirmation have been entered and they match.
            if (login.Password == login.ConfirmPassword && login.Password != "")
            {
                // Send the new password to the api to be encrypted and written to the User table.
                userModel.NewPassword = login.Password;
                var content = new StringContent(JsonConvert.SerializeObject(userModel), Encoding.UTF8, mediaType);
                string routePath = $"{routePathBase}UpdatePassword";
                var response = AuthorisedClient.PutAsync(routePath, content).Result;
                var result = await response.Content.ReadAsStringAsync();

                // If all successful redirect the user to the login to relogin with new password.
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index", "Login");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError("Error", "Error Try again Later");
                }
            }
            else
            {
                ModelState.AddModelError("Error", "Passwords do not match");
            }
        }
        else
        {
           ModelState.AddModelError("Error", "Invalid user credentials - password requires at least 1" +
               " special character and a minimum of 8 characters in total");
        }
        
        return View();

    }
    // Check the User is setup, generate a code, write it to TempUser table
    // and send email.
    public async Task<ActionResult> EmailCode(LoginModel loginModel)
    {
        // A check to see if the user is logged in 
        if(!HttpContext.Session.GetInt32(SessionKeys.UserID).HasValue)
        {
            var token = await GetTokenAsync();
            HttpContext.Session.SetString(SessionKeys.UserTokenLocalAPI, token);
        }
        
        string errorMsg = "Contact your Administrator for further details.";
        string error1Msg = "There was a problem allocating a verification code. " + errorMsg;
        string error2Msg = "User credentials do not exist. " + errorMsg;
        string error3Msg = "A verification code has been sent to your inbox";
        string error4Msg = "Incorrect email verification code. Please try again.";
        string error5Msg = "There was a problem sending to the recipient email address. " + errorMsg;

        UserModel userModel = new UserModel();

        if (loginModel.Email != null && loginModel.Email != "")
        {
        
            userModel = await GetUserAsync(loginModel.Email);
            
            if (userModel == null)
            {
                ModelState.AddModelError("Error", error2Msg);
                return View();
            }

        }
        
        // send code button entered
        if (loginModel.Email != null && loginModel.CodeEntered == 0)
        {
            int code = GetRandomCode();
            
            if (code > 0)
            {
                TempUserModel tempUserModel = new TempUserModel
                {
                    ConfirmationCode = code,
                    Id = userModel.Id,
                };

                if (WriteCode(tempUserModel))
                {
                    if (sendEmail(userModel, tempUserModel))
                    {
                        HttpContext.Session.SetString(nameof(LoginModel.Email), loginModel.Email);
                        ModelState.AddModelError("Error", error3Msg);
                    }
                    else
                    {
                        ModelState.AddModelError("Error", error5Msg);
                    }
                }
                else
                {
                    ModelState.AddModelError("Error", error1Msg);
                }

            }
            else
            {
                ModelState.AddModelError("Error", error1Msg);
            }

        // Confirm button entered code 
        }
        else if (loginModel.Email != null && loginModel.CodeEntered > 0)
        {
            TempUserModel tempUserModel = GetTempUserAsync(loginModel.Email).Result;
            bool success = false;

            if (tempUserModel != null && tempUserModel.VerifyHash(loginModel.CodeEntered.ToString()))
            {
                success = true;
                HttpContext.Session.SetString(nameof(LoginModel.Email), loginModel.Email);
            }
            else
            {
                ModelState.AddModelError("Error", error4Msg);
            }

            // Clear the code so user can not reuse same code over and over again without 
            // sending a new email.
            tempUserModel.EncryptedCode = "";
            tempUserModel.ConfirmationCode = 0;
            WriteCode(tempUserModel);

            if (success)
            {
                return View("NewLogin");
            } else
            {
                return View();
            }

        }
        
        return View();
    }

    public async Task<string> GetTokenAsync()
    {
        var token = "";
        var login = new ServerLoginModel();
        login.UserName = _config["ManualServerLogin:UserName"];        
        login.Password = _config["ManualServerLogin:Password"];
        var content = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, mediaType);
        var response = Client.PostAsync($"{routePathBase}login", content).Result;

        if(response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            token = JsonConvert.DeserializeObject<string>(result);
         
        }
        return token;
    }
            
    public bool sendEmail(UserModel userModel,TempUserModel tempUserModel)
    {
        EmailManager emailManager = new EmailManager(_config);
        EmailModel emailModel = new EmailModel();

        emailModel.From = "donotreply@9b12558b-8d6c-473d-9a9e-e9f4bc8f5385.azurecomm.net";
        emailModel.Body = $"Hi {userModel.FullName.Split(' ')[0]},\n\nPlease find the below" +
            $" 4 digit code to verify your email address with us." +
            $"\n\n\n       {tempUserModel.ConfirmationCode}" +
            "\n\n\nKind Regards,\n" +
            "The LeafLinq team.\n\n\n" +
            "Please do not reply to this email.\n" +
            "This email address is not monitored.\n" +
            "Please contact your Administrator for further details.";
        emailModel.Subject = "No reply - leafLINQ email verification code";
        emailModel.To.Add(userModel.Email);

        return emailManager.sendEmailUsingAzure(emailModel);
        //return emailManager.sendEmailUsingSlurp(userModel,tempUserModel);
                
    }
        
    public IActionResult Index(LoginModel loginModel)
    {
        HttpContext.Session.SetString(nameof(LoginModel.Email), loginModel.Email);
        return View();
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
                ModelState.AddModelError("Error", "Error Try again Later");
                return null;
            }
        }
        ModelState.AddModelError("Error", "Error Try again Later");
        return null;
    }

    public async Task<TempUserModel> GetTempUserAsync(String email)
    {
        
        if (email != "")
        {
            string getPath = $"{routePathBase}GetTempUser?email=" + email;
            var response = AuthorisedClient.GetAsync(getPath).Result;
            var result = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonConvert.DeserializeObject<TempUserModel>(result);
            
            if (response.IsSuccessStatusCode)
            {
                return loginResponse;
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError("Error", "Error Try again Later");
                return null;
            }
        }

        ModelState.AddModelError("Error", "Error Try again Later");
        return null;
    }
    public int GetRandomCode()
    {
        Random rnd = new Random();

        // First digit is 1 to 10 so we do not get a 0 which gets truncated as its an int.
        // I'm too lazy to change it to a string.
        string digit1 = (string)rnd.Next(1, 10).ToString();
        string digit2 = (string)rnd.Next(0, 10).ToString();
        string digit3 = (string)rnd.Next(0, 10).ToString();
        string digit4 = (string)rnd.Next(0, 10).ToString();

        string code = digit1 + digit2 + digit3 + digit4;

        return Int32.Parse(code);

    }

    public bool WriteCode(TempUserModel tempUserModel)
    {
        if ( tempUserModel != null )
        {
            string routePath = $"{routePathBase}WriteTempUser";
            var content = new StringContent(JsonConvert.SerializeObject(tempUserModel), Encoding.UTF8, mediaType);
            string bodyS = content.ReadAsStringAsync().Result;
            var response = AuthorisedClient.PutAsync(routePath, content).Result;
           
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        
        ModelState.AddModelError("Error", "Error Try again Later");
        return false;

    }

    //public void DM(string message)
    //{
    //    Console.WriteLine($"\n{message}\n");
    //}

}