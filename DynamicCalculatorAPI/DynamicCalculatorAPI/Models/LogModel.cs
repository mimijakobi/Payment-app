using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicCalculatorAPI.Models
{
    [Table("t_log")]
    public class LogModel
    {
        

        [Key]
        public int log_id { get; set; }
        public Guid job_id { get; set; }
        public int targil_id { get; set; }
        public string method { get; set; } 
        public float run_time { get; set; } 
    }
}
