using DynamicCalculatorAPI.Models;

namespace DynamicCalculatorAPI.Interfaces
{
    public interface ICalculateService
    {
        Task<List<MethodComparisonDto>> GetComparisonReportAsync(Guid jobId);
    }
}
