using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicCalculatorAPI.Models
{
    [Table("Jobs")]
    public class JobModel
    {
        [Key]
        public Guid JobId { get; set; }
        public string? Status { get; set; } // pending / running / completed / failed
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
