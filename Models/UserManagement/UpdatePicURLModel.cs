using System.ComponentModel.DataAnnotations;

namespace LeafLINQWeb.Models.UserManagement;

public class UpdatePicURLModel
{

    public int Id { get; set; }
    [StringLength(200), Display(Name = "Profile Image"), Required]
    public string PicUrl { get; set; }

}
