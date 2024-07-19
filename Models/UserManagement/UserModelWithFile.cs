using System.ComponentModel.DataAnnotations;

namespace LeafLINQWeb.Models.UserManagement;

public class UserModelWithFile
{
    [Required]
    public int Id { get; set; }
    [Required, StringLength(40), RegularExpression(@"^[a-zA-Z\s'.-]+$", ErrorMessage = "Letters Only Please"), Display(Name = "Full Name")]
    public string FullName { get; set; }
    [Required, StringLength(40), RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a Valid Email Please")]
    public string Email { get; set; }
    public string PicUrl { get; set; }
    public IFormFile ImageFile {get; set; }
    //[Required, StringLength(200)]
    //public string Desc { get; set; }
    [Required, RegularExpression(@"^[AU]$", ErrorMessage = "Must be Either A or U")]
    public char UserType { get; set; }
    [Required]
    public DateTime LastLoginDate { get; set; }

}
