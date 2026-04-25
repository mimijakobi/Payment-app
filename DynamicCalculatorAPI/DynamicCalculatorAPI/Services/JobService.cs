using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;
using DynamicCalculatorAPI.Repository;

namespace DynamicCalculatorAPI.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepository;
        public JobService(IJobRepository _jobRepository)
        {
            this._jobRepository = _jobRepository;
        }
        public async Task<Guid> CreateJobAsync()
        {
            return await _jobRepository.CreateJobAsync();
        }

        public async Task UpdateStatusAsync(Guid jobId, string status, string error = null)
        {
            await _jobRepository.UpdateStatusAsync(jobId, status, error);
        }

        public async Task<JobStatusDto> GetStatusAsync(Guid jobId)
        {
            return await _jobRepository.GetStatusAsync(jobId);
        }


    }   


}

