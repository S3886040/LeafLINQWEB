namespace LeafLINQWeb.Models;

public class SessionKeys
{
    // User token for the JWT encryption key value stored in session. Used to send with the 
    // "Auth" "api" call (local)
    public const string UserTokenLocalAPI = "JWTTokenWebAPI";

    // User token for the JWT encryption key value stored in session. Used to send with the 
    // "api-plant-data" call (external embedded api. eg api/GetDevices, api/Plants) 
    public const string UserTokenExternalAPI = "JWTTokenPlantAPI";

    // The refresh token is used as a longer service token to authenticate refreshment of the standard short lived jwt token
    public const string RefreshTokenLocalAPI = "RefreshTokenWebAPI";

    // Scheduler token for the JWT encryption key value stored in session. Used to send with 
    // the call to "scheduler-api-plant-data" (external embedded api. eg api/GetDevices, api/Plants)
    public const string SchedulerToken = "JWTTokenSchedulerPlantAPI";

    // Used to define the session, to allow multiple logged experiences for the user.
    public const string SessionID = "SessionID";

    // User Type 
    public const string UserType = nameof(UserModel.UserType);

    // User id
    public const string UserID = nameof(UserModel.Id);

    // User full name.
    public const string UserFullName = nameof(UserModel.FullName);

    // User Picture URL for profile image
    public const string UserPicUrl = nameof(UserModel.PicUrl);

}                    

                        

    
