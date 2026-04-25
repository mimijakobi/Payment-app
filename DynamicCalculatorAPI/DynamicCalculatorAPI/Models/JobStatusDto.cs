namespace DynamicCalculatorAPI.Models
{
    public class JobStatusDto
    {
        public Guid JobId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

}
