namespace LeafLINQWeb.Models;

public class RefreshTokenModel
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string SessionId { get; set; }
}
