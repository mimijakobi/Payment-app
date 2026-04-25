using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicCalculatorAPI.Models
{
    [Table("results")]
    public class ResultModel
    {
        [Key ]
        public int result_id { get; set; }
        public Guid job_id { get; set; }
        public int data_id { get; set; }
        public int targil_id { get; set; }
        public string method { get; set; }
        public float result { get; set; }
    }
}
