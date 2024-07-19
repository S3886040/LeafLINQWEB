using LeafLINQWeb.Models;
using Newtonsoft.Json;
using System.Text;
using LeafLINQWeb.Controllers;
using DotLiquid;
using System;
using Azure;

namespace LeafLINQWeb.Scheduler;

public class PlantAlertsScheduler : BackgroundService
{

    private readonly ILogger<PlantAlertsScheduler> _Logger;

    // External api to the web page separate database. Hardware side devices
    private readonly HttpClient DeviceAPI;

    // Internal api for the web page separate database to the hardware side.
    // where plant and user data is stored.
    private readonly HttpClient PlantAPI;

    // Internal api for getting the JWTToken for our users. Included into the header for
    // all api requests. See WebAPICustomHandler
    private readonly HttpClient PlantAuthAPI;

    //private readonly EmailManager EmailManager;

    private readonly IConfiguration _config;

    private const int SleepTime = 45;

    public PlantAlertsScheduler(ILogger<PlantAlertsScheduler> logger, IHttpClientFactory clientFactory, IConfiguration config)
    {
        _Logger = logger;
        DeviceAPI = clientFactory.CreateClient("api-plant-data");
        PlantAPI = clientFactory.CreateClient("api");
        PlantAuthAPI = clientFactory.CreateClient("auth");
        _config = config;
    }

    // standard background scheduler call. 
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        LogInfo("Starting plant update...");

        while (!cancellationToken.IsCancellationRequested)
        {

            try
            {
                await PlantUpdatesAsync(cancellationToken);

            }
            catch (Exception ex)
            {
                _Logger.LogError("Unexpected exception occurred processing PlantUpdatesAsync\n"
                                + ex.Message + "\n"
                                + ex.StackTrace);
            }
            finally
            {
                string addS = "";
                if(SleepTime > 1) { addS = "s"; }
                LogInfo($"Plant API Scheduler is sleeping for {SleepTime} minute{addS}");
                await Task.Delay(TimeSpan.FromMinutes(SleepTime), cancellationToken);
            }

        }
    }

    // Main update routine.
    private async Task PlantUpdatesAsync(CancellationToken cancellationToken)
    {
        int maxSteps = 5;
        bool Ok = true;

        // list of all device information found within plant API
        List<PlantAPIDeviceModel> devices = new List<PlantAPIDeviceModel>();

        // list of all locally stored plants.
        List<PlantModel> plants = new List<PlantModel>();

        // list of all devices sensor readings.
        List<PlantSensorReadings> plantSensorReadings = new List<PlantSensorReadings>();

        LoginResponse loginTokenResponse = null;
        PlantResponse plantTokenResponse = null;

        // start main logic loop.
        for (int stepCount = 1; stepCount <= maxSteps && Ok; stepCount++)
        {

            switch (stepCount)
            {
                case 1:

                    IEnumerable<IResponse> responseList = await GetAllTokensAsync();

                    foreach (IResponse iResp in responseList)
                    {
                        if (iResp is LoginResponse)
                        {
                            loginTokenResponse = (LoginResponse)iResp;
                        }
                        else if (iResp is PlantResponse)
                        {
                            plantTokenResponse = (PlantResponse)iResp;
                        }
                    }

                    if (loginTokenResponse == null || plantTokenResponse == null) Ok = false;

                    break;

                case 2:

                    // Make sure we built the JWT External and the Local (ours) Token
                    if (plantTokenResponse.jwtToken != null && loginTokenResponse.AccessToken != "")
                    {

                        PseudoSession.Instance.BackgroundExternalAPIToken = plantTokenResponse.jwtToken;
                        PseudoSession.Instance.BackgroundLocalAPIToken = loginTokenResponse.AccessToken;

                        devices = (List<PlantAPIDeviceModel>)await GetAllDevices();

                        //foreach (PlantAPIDeviceModel device in devices)
                        //{
                        //    _Logger.LogInformation($"\n{device}\n");
                        //}
                    }

                    if (devices.Count <= 0)
                    {
                        Ok = false;
                        LogInfo("Failed to get any devices");
                    }

                    break;

                case 3:

                    plants = (List<PlantModel>) await GetAllPlants();

                    break;

                case 4:

                    plantSensorReadings = (List<PlantSensorReadings>)await GetAllSensorReadings();
                    plantSensorReadings = (List<PlantSensorReadings>) PurgePlantSensorReadings(plantSensorReadings);

                    if (plantSensorReadings.Count > 0)
                    {
                        UpdatePlantStatus(plantSensorReadings, plants);
                    }
                    break;

                case 5:

                    bool wrote = await CheckPlants(plants, devices, plantSensorReadings);

                    break;
            }

        }

    }

    // Get all of the authorise tokens required to execute any http request to 
    // local or external api.
    private async Task<IEnumerable<IResponse>> GetAllTokensAsync()
    {
        List<IResponse> tokens = new List<IResponse>();
        try
        {
            PlantResponse plantResponse = await GetDeviceAPIToken();
            if (plantResponse.jwtToken != null)
            {
                tokens.Add(plantResponse);
            }
            else
            {
                LogInfo($"Missing plant-api token.");
            }

            LoginResponse loginResponse = await GetPlantAPIToken();

            if (loginResponse.AccessToken != null)
            {
                tokens.Add(loginResponse);
            }
            else
            {
                LogInfo($"Missing api token.");
            }
        }catch(Exception){}

        if(tokens.Count == 2)
        {
            LogInfo($"All JWT tokens and associated logins loaded successfully");
        } else
        {
            LogInfo($"An error occurred loading JWT tokens and associated logins");
        }

        return tokens;
    }

    // get the JWT for the plant API data.
    private async Task<PlantResponse> GetDeviceAPIToken()
    {

        AuthentificationLoginModel authLoginModel = new AuthentificationLoginModel();
        
        StringContent content = new StringContent(JsonConvert.SerializeObject(authLoginModel), Encoding.UTF8, "application/json");
       
        string routePath = "api/Auth/login";
        PlantResponse plantResponse = null;
        HttpResponseMessage responsePlantAPI = new HttpResponseMessage();

        try
        {
            responsePlantAPI  = await DeviceAPI.PostAsync(routePath, content);
            string resultPlantAPI = await responsePlantAPI.Content.ReadAsStringAsync();
            plantResponse = JsonConvert.DeserializeObject<PlantResponse>(resultPlantAPI);
            LogInfo($"Device/Hardware API server token retrieved");

        } catch (Exception ex)
        {
            LogError("POST",routePath,plantResponse,ex);
        }

        return plantResponse;
    }

    // get the JWT for local API
    private async Task<LoginResponse> GetPlantAPIToken()
    {
        AuthentificationLoginModel authLoginModel = new AuthentificationLoginModel();
        authLoginModel.SetSchedulerUserNamePassword();

        LoginModel loginModel = new LoginModel
        {
            Email = authLoginModel.GetUserName(),
            Password = authLoginModel.GetPassword(),
        };

        StringContent content = new StringContent(JsonConvert.SerializeObject(loginModel), Encoding.UTF8, "application/json");
        
        HttpResponseMessage responseLocalAPI = new HttpResponseMessage();
        LoginResponse loginResponse = new LoginResponse();
        
        string routePath = "api/login";
        
        try {                 
            responseLocalAPI = PlantAuthAPI.PostAsync(routePath, content).Result;
            var result = await responseLocalAPI.Content.ReadAsStringAsync();
            loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
            LogInfo($"Plant/User API server token retrieved");
        } 
        catch (Exception ex)
        {
            LogError("POST",routePath,loginResponse,ex);
        }

        return loginResponse;

    }

    // get all plants stored within web app database.
    private async Task<IEnumerable<PlantModel>> GetAllPlants()
    {
        string routePath = "api/Plants/allPlants";
        List<PlantModel> plants = new List<PlantModel>();
        HttpResponseMessage response = new HttpResponseMessage();
                    
        try
        {
            response = await PlantAPI.GetAsync(routePath);
        
        } catch (Exception ex)
        {
            LogError("GET", routePath, response, ex);
        }

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and deserialize the response content
            string result = await response.Content.ReadAsStringAsync();
            plants = JsonConvert.DeserializeObject<List<PlantModel>>(result);
            LogInfo($"Plant data retrieved count: {plants.Count}");
        }
        else
        {
            LogError("GET", routePath, response, null);
        }

        return plants;
    }

    // get all devices.
    private async Task<IEnumerable<PlantAPIDeviceModel>> GetAllDevices()
    {
        string routePath = "api/Devices";
        string result = string.Empty;
        HttpResponseMessage response = new HttpResponseMessage();
        IEnumerable<PlantAPIDeviceModel> devices = null;

        try
        {
            response = DeviceAPI.GetAsync(routePath).Result;
            
        }
        catch (Exception ex)
        {
            LogError("GET", routePath, response, ex);
        }

        if (response.IsSuccessStatusCode)
        {
            result = await response.Content.ReadAsStringAsync();
            devices = JsonConvert.DeserializeObject<IEnumerable<PlantAPIDeviceModel>>(result);
            LogInfo($"Device data retrieved count: {devices.Count()}");
        }
        else
        {
            LogError("GET", routePath, response, null);
        }
        return devices;
    }

    // send the plant id to the plant API to associate with the device.
    private bool WritePlantToDevice(string _plantID, string _deviceID)
    {
        string routePath = "api/Plants";
        HttpResponseMessage response = new HttpResponseMessage();

        BodyResponsePlantDeviceModel bodyResponsePlantDeviceModel = new BodyResponsePlantDeviceModel
        {
            plantID = _plantID,
            deviceID = _deviceID
        };
       
        try
        {
            StringContent contentPlantAPI = new StringContent(JsonConvert.SerializeObject(bodyResponsePlantDeviceModel), Encoding.UTF8, "application/json");
            response = DeviceAPI.PostAsync(routePath, contentPlantAPI).Result;
            var result = response.Content.ReadAsStringAsync();
            LogInfo($"Device to Plant assigned plant ID: {_plantID} DeviceID: {_deviceID}");
        }
        catch(Exception ex)
        {
            LogError("POST", routePath, response, ex);
        }   
 
        if (response.IsSuccessStatusCode)
        {
            return true;
        } else
        {
            // Just in case we didn't get an exception
            LogError("POST", routePath, response, null);
            return false;
        }
    }

    // check the plant has a device id recorded verify that the plant id is correctly recorded against 
    // the device with the plant API. Then check that the device is running OK isInError flag on the device.
    // Then check the plant status is healthy. If the plant is not healthy or the device is in error then
    // send an email to the applicable user. 
    public async Task<bool> CheckPlants(IEnumerable<PlantModel> plants, 
        IEnumerable<PlantAPIDeviceModel> devices, IEnumerable<PlantSensorReadings> plantSensorReadings) 
    {
        int count = 1;
        bool wrote = false;
        bool sendEmail = false;
     
        foreach (PlantModel plant in plants)
        {
            // if the device id has a value
            if (plant.DeviceId != "" && plant.DeviceId != null)
            {
                LogInfo($"Plant {plant.Id} {plant.Desc} has device {plant.DeviceId}");
                // loop through the devices and find the device
                foreach (var device in devices)
                {

                    if (plant.DeviceId == device.deviceId)
                    {
                        LogInfo($"Found device {count} : {device.deviceId}");
                        // write the correct plant id to the device
                        if ((device.plantId == null || device.plantId == "") || (Int32.Parse(device.plantId) != plant.Id))
                        {
                            wrote = WritePlantToDevice(plant.Id.ToString(), device.deviceId);
                            if (wrote)
                            {
                                device.plantId = plant.Id.ToString();
                                LogInfo($"\nSent plant id message to plant API for Device {count}" +
                                    $"\n{device}" +
                                    $"\n{plant}");
                            }
                            break;
                        }

                        PlantSensorReadings sensorData = GetSensorData(plantSensorReadings, plant);

                        if (CheckDeviceError(device))
                        {
                            LogInfo($"Detected error in device node. Device ID: {device.deviceId} Plant ID: {plant.Id}");
                            sendEmail = true;
                            //bool sent = await sendEmailAsync(device, plant, sensorData);
                        }
                        if (CheckPlantStatus(plant))
                        {
                            LogInfo($"Detected plant unhealthy status. Device ID: {device.deviceId} Plant ID: {plant.Id}");
                            sendEmail = true;
                            //bool sent = await sendEmailAsync(device, plant, sensorData);
                        }

                        if (sendEmail)
                        {
                            bool sent = await sendEmailAsync(device, plant, sensorData);
                            sendEmail = false;
                        }
                        count++;
                    }

                    
                }
            }
        }

        return wrote;

    }

    // check the isInError flag. Indicates if the device is faulty/not responding.
    public bool CheckDeviceError(PlantAPIDeviceModel device)
    {
        if (device.isInError.ToLower() == "true")
        {
            return true;
        }
        return false;
    }

    // Cycle through all of the plant sensor readings and update the status accordingly.
    // Soil moisture < 50% set alert
    // Soil moisture < 25% set critical
    // Humidity < 50% set alert
    // Humidity < 25 set critical. Light intensity and temperature not necessary just observational data at present.
    public void UpdatePlantStatus(IEnumerable<PlantSensorReadings> plantReadings, IEnumerable<PlantModel> plantModels)
    {
        List<PlantSensorReadings> plantSensorReadings = (List<PlantSensorReadings>)PurgePlantSensorReadings(plantReadings);

        foreach(var plantSensed in plantSensorReadings)
        {
            foreach (var plantModel in plantModels)
            {
                if (plantModel.DeviceId == plantSensed.deviceId)
                {
                    // check whether the plants health has changed update plantModel if it has.
                    if (plantModel.UpdateHealthStatus(plantSensed))
                    {
                        WritePlant(plantModel);
                    }
                }
            }
        }
    }

    // Get a reduced list of plant readings anything without a plant assigned we do not need.
    public IEnumerable<PlantSensorReadings> PurgePlantSensorReadings(IEnumerable<PlantSensorReadings> plantReadings)
    {
        List<PlantSensorReadings> newPlantReadings = new List<PlantSensorReadings>();

        foreach(var plantSensor in plantReadings)
        {

            //if(plantSensor.plantId != "null" && plantSensor.plantId != "" && plantSensor.plantId != null)
            //{
            //    LogBasicInfo($"Added plantId '{plantSensor.plantId}'");
            //    newPlantReadings.Add(plantSensor);
            //}
            // The above logic should be applicable, however the sensor reading data does not have the plant id in it at 
            // present.
            newPlantReadings.Add(plantSensor);
        }

        return newPlantReadings;
    }

    // get the matching sensor data for this plant.
    public PlantSensorReadings GetSensorData(IEnumerable<PlantSensorReadings> plantSensorReadings, PlantModel plant)
    {
        PlantSensorReadings sensorMatch = new PlantSensorReadings();

        foreach (var sensor in plantSensorReadings)
        {
            if (sensor.plantId == plant.Id.ToString())
            {
                sensorMatch = sensor;
                break;
            }
        }
        return sensorMatch;
    }

    // check the plants health status. If healthy return true
    public bool CheckPlantStatus(PlantModel plant)
    {
        
        if(plant.HealthCheckStatus != HealthCheckStatus.Healthy)
        {
            return true;
        }
        return false;
    }

    // get all sensor readings and populate list
    public async Task<IEnumerable<PlantSensorReadings>> GetAllSensorReadings()
    {
        string routePath = "api/SensorReadings";
        HttpResponseMessage response = new HttpResponseMessage();
        string result = string.Empty;
        IEnumerable<PlantSensorReadings> plantModels = null;

        try
        {
            response = DeviceAPI.GetAsync(routePath).Result;
         
        }catch(Exception ex)
        {
            LogError("GET", routePath, response, ex);
        }

        if (response.IsSuccessStatusCode)
        {
            result = await response.Content.ReadAsStringAsync();
            plantModels = JsonConvert.DeserializeObject<IEnumerable<PlantSensorReadings>>(result);
            LogInfo($"Device sensor readings retrieved count: {plantModels.Count()}");
        }
        else
        {
            // Just in case we didn't get an exception
            LogError("GET", routePath, response, null);
        }

        return plantModels;
    }

    // update the plant information.
    public bool WritePlant(PlantModel plant)
    {
        string routePath = "api/plants/updatePlant";
        HttpResponseMessage response = new HttpResponseMessage();
        
        try
        {
            StringContent contentPlantAPI = new StringContent(JsonConvert.SerializeObject(plant), Encoding.UTF8, "application/json");
            response = PlantAPI.PutAsync(routePath, contentPlantAPI).Result;
            var result = response.Content.ReadAsStringAsync();
            LogInfo($"Update Plant record plantID: {plant.Id} plant deviceID: {plant.DeviceId} health status changed to: {plant.HealthCheckStatus}");
        }
        catch (Exception ex)
        {
            LogError("PUT", routePath, response, ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            // Just in case we didn't get an exception
            LogError("PUT", routePath, response, null);
            return false;
        }
    }

    // send an email to the relevant user if plant status is unhealthy or the device is in error.
    public async Task<bool> sendEmailAsync(PlantAPIDeviceModel device, PlantModel plant, PlantSensorReadings sensorData)
    {

        EmailManager emailManager = new EmailManager(_config);
        EmailModel emailModel = new EmailModel();
        UserModel userModel = await GetUserAsync(plant.UserId);

       
        emailModel.From = "donotreply@9b12558b-8d6c-473d-9a9e-e9f4bc8f5385.azurecomm.net";
                
        string bodyText = string.Empty;

        string liquidTemplateContent = File.ReadAllText("wwwroot/Email_HTML/PlantAlert.liquid");

        // then we parse the contents of the template file into a liquid template
        Template template = Template.Parse(liquidTemplateContent);

        EmailAlertMsg emailAlertMsg = new EmailAlertMsg
        {
            First_Name = userModel.FullName.Split(' ')[0],
            Device = device.ToString(),
            Plant = plant.ToString(),
            Sensor_Data = sensorData.ToString(),
        };

        Hash hash = Hash.FromAnonymousObject(new
        {
            emailAlertMsg = new EmailAlertMsgDrop(emailAlertMsg)
        });

        bodyText = template.Render(hash);
       
        emailModel.Body = bodyText; 
        emailModel.Subject = "No reply - leafLINQ plant device alert";
        emailModel.To.Add(userModel.Email);

        return emailManager.sendEmailUsingAzure(emailModel);
        //return emailManager.sendEmailUsingSlurp(userModel,tempUserModel);
    }
    
    // get a user record from User table
    public async Task<UserModel> GetUserAsync(int userID)
    {
        string routePath = "recovery/GetUserUsingUserID?userID=" + userID; 
        string result = string.Empty;
        HttpResponseMessage response = new HttpResponseMessage();
        UserModel user = null;

        try
        {
            response = PlantAPI.GetAsync(routePath).Result;
            result = await response.Content.ReadAsStringAsync();
            user = JsonConvert.DeserializeObject<UserModel>(result);
            LogInfo($"Retrieved UserID: {user.Id} User Name {user.FullName} User email: {user.Email}");
        }
        catch (Exception ex)
        {
            LogError("GET", routePath, response, ex);
        }
        return user;
    }
    
    // log an error using standard logger
    public void LogError(string type, string routePath, Object response, Exception ex)
    {
        string indent = new string(' ', 6);
        string errorMsg = "No Exception";

        if (ex != null)
        {
            errorMsg = ex.Message;
        } 
        
        if (response is HttpResponseMessage)
        {
            
            HttpResponseMessage responseMessage = (HttpResponseMessage)response;
            _Logger.LogError($"\n{indent}Connection error - while {type}ing {routePath}\n" +
            $"{indent}Error returned - {errorMsg}\n" +
            $"{indent}Response status code: {responseMessage.StatusCode}\n" +
            $"{indent}Response Phrase: {responseMessage.ReasonPhrase}\n");

        } else if(response is LoginResponse)
        {
            
            LoginResponse loginResponse = (LoginResponse)response;
            _Logger.LogError($"\n{indent}Connection error - while {type}ing {routePath}\n" +
            $"{indent}Error returned - {errorMsg}\n" +
            $"{indent}LoginResponse - Id = {loginResponse.Id}\n" +
            $"{indent}LoginResponse - Type = {loginResponse.UserType}\n");

        } else if(response is PlantResponse)
        {
            
            PlantResponse plantResponse = (PlantResponse)response;
            _Logger.LogError($"\n{indent}Connection error - while {type}ing {routePath}\n" +
            $"{indent}Error returned - {errorMsg}\n" +
            $"{indent}PlantRespone - Type {plantResponse.GetType()}\n");

        }
    }

    // log information messages using standard logger.
    public void LogInfo(string msg)
    {
        string indent = new string(' ', 6);
        _Logger.LogInformation($"\n{indent}{msg}\n");
    }
}