using System.ComponentModel.DataAnnotations;

namespace LeafLINQWeb.Models;

public class UserModel
{

    public int Id { get; set; }

    [Required, StringLength(40), DataType(DataType.Text, ErrorMessage = "Non text based data encountered."), Display(Name = "Full Name")]
    public string FullName { get; set; } = null!;

    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a Valid Email Please"),
        StringLength(50), Display(Name = "Email"), Required]
    public string Email { get; set; } = null!;

    [StringLength(200), Display(Name = "Profile Image"), Required]
    public string PicUrl { get; set; } = null!;

    [RegularExpression(@"^[UA]$", ErrorMessage = "Must be (U)ser or (A)ccount manager"), Display(Name = "Type")]
    public char UserType { get; set; }

    [Display(Name = "Last Login Date"), Required]
    public DateTime LastLoginDate { get; set; }
    public bool Block { get; set; }

    public string NewPassword { get; set; }

}