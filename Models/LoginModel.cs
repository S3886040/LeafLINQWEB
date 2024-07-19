using System.ComponentModel.DataAnnotations;
namespace LeafLINQWeb.Models;

[Serializable]
public class LoginModel
{
    public string Email { get; set; }

    //[RegularExpression(@"^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")]
    // Thank you chatGPT
    [RegularExpression(@"^(?=.*[!@#$%^&*()_+{}\[\]:;<>,.?/~`\-|\\]).{8,}$")]
    public string Password { get; set; }

    //[RegularExpression(@"^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z0-9]).{8,}$")]
    [RegularExpression(@"^(?=.*[!@#$%^&*()_+{}\[\]:;<>,.?/~`\-|\\]).{8,}$")]
    public string ConfirmPassword { get; set; }
    public int CodeEntered { get; set; }
}
