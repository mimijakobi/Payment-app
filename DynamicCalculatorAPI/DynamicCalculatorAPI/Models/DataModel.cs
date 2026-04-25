using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicCalculatorAPI.Models
{
    [Table("t_data")]
    public class DataModel     
    {
        [Key]
        public int data_id { get; set; }
        public float A { get; set; }
        public float B { get; set; }
        public float C { get; set; }
        public float D { get; set; }
    }
}
