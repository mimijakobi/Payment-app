using DynamicCalculatorAPI.DBContext;
using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;

namespace DynamicCalculatorAPI.Repository
{
    public class JobRepository : IJobRepository
    {
        private readonly PaymentContext _context;

        public JobRepository(PaymentContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateJobAsync()
        {
            var job = new JobModel
            {
                JobId = Guid.NewGuid(),
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Job.Add(job);
            await _context.SaveChangesAsync();

            return job.JobId;
        }

        public async Task UpdateStatusAsync(Guid JobId, string status, string error = null)
        {
            var job = await _context.Job.FindAsync(JobId);
            if (job == null)
                return;

            job.Status = status;
            job.ErrorMessage = error;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<JobStatusDto> GetStatusAsync(Guid JobId)
        {
            var job = await _context.Job.FindAsync(JobId);

            if (job == null)
                return null;

            return new JobStatusDto
            {
                JobId = job.JobId,
                Status = job.Status,
                ErrorMessage = job.ErrorMessage
            };
        }
    }

}
