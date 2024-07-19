namespace LeafLINQWeb.Models;

public class AuthentificationLoginModel
{
    // default values for the external API to get the JWT token.
    private const string _username = "webportal";
    private const string _password = "password321";
    public string username { get; set; }
    public string password { get; set; }

    public AuthentificationLoginModel(string iUserName, string iPassword) 
    {
        DefaultValues();

        // Override username and password with constructor parameters if passed.
        if (iUserName != null && iUserName != "") username = iUserName;
        if (iPassword != null && iPassword != "") password = iPassword;
               
    }

    public AuthentificationLoginModel() 
    {
        DefaultValues();
    }

    private void DefaultValues()
    {
        // Default username and password.
        username = _username;
        password = _password;
    }

    public string GetUserName() 
    { 
        return username;
    }

    public string GetPassword() 
    {
        return password;
    }

    // Allow for the resetting of the username and password. Not sure what for but, whatever...
    public void SetUserNamePassword(string iPassword, string iUserName)
    {
        username = iPassword;
        password = iUserName;
    }

    public void SetSchedulerUserNamePassword()
    {
        username = "Scheduler@LeafLINQ.com";
        password = "scheduler1234567*";
    }
}
