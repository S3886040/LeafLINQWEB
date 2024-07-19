namespace LeafLINQWeb.Models;

public class LoginResponse : IResponse
{
    public string UserType { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int Id { get; set; }
    public string SessionID { get; set; }
}
