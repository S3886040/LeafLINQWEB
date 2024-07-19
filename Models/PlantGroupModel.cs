using System.ComponentModel.DataAnnotations;

namespace LeafLINQWeb.Models
{
    public partial class PlantGroupModel
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
        public string Desc { get; set; }
    }
}
