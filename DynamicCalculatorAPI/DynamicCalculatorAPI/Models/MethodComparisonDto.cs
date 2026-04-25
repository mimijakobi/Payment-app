namespace DynamicCalculatorAPI.Models
{
    public class MethodComparisonDto
    {
        public string method { get; set; }
        public double total_run_time { get; set; }
        public double avg_run_time { get; set; }
        public double relative_speed { get; set; }
        public bool is_fastest { get; set; }
        public bool results_match { get; set; }

    }

}
