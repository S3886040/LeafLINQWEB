using System.ComponentModel.DataAnnotations;

namespace LeafLINQWeb.Models;

public class ProfileUpdateModel
{

    [Required]
    public int Id { get; set; }
    [Required, StringLength(40), RegularExpression(@"^[a-zA-Z\s'.-]+$", ErrorMessage = "Letters Only Please")]
    public string FullName { get; set; }
    [Required, StringLength(40), RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a Valid Email Please")]
    public string Email { get; set; }
    public string PicUrl { get; set; }
    public IFormFile ImageFile { get; set; }

}
