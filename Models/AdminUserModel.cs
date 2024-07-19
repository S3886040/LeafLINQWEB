using LeafLINQWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LeafLINQWeb.Models;

public class AdminUserModel
{
    public UserModel User { get; set; }
    public List<PlantModel> Plants { get; set; }
    public List<SelectListItem> AllPlants { get; set; }
}
