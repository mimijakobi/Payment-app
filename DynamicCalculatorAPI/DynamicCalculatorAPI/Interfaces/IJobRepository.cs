using DynamicCalculatorAPI.Models;

namespace DynamicCalculatorAPI.Interfaces
{
    public interface IJobRepository
    {
        Task<Guid> CreateJobAsync();
        Task UpdateStatusAsync(Guid jobId, string status, string error = null);
        Task<JobStatusDto> GetStatusAsync(Guid jobId);
    }

}
