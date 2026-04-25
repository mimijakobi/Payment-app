using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicCalculatorAPI.Models
{
    [Table("t_targil")]
    public class TargilModel
    {
        [Key]
            public int targil_id { get; set; }
            public string targil { get; set; }       // targil
            public string? tnai { get; set; }    // tnai (nullable)
            public string? targil_false { get; set; } // targil_false (nullable)
        
    }
}
