using DynamicCalculatorAPI.Interfaces;

public class EngineOrchestrator : IEngineService
{
    private readonly IEnumerable<IMethodEngine> _engines;
    private readonly IJobRepository _jobRepository;

    public EngineOrchestrator(IEnumerable<IMethodEngine> engines,
                              IJobRepository jobRepository)
    {
        _engines = engines;
        _jobRepository = jobRepository;
    }

    public async Task RunAsync(Guid jobId,int? limit = null)
    {
        await _jobRepository.UpdateStatusAsync(jobId, "running");

        try
        {
            var tasks = _engines.Select(e => e.RunAsync(jobId,limit));
            await Task.WhenAll(tasks);

            await _jobRepository.UpdateStatusAsync(jobId, "completed");
        }
        catch (Exception ex)
        {
            await _jobRepository.UpdateStatusAsync(jobId, "failed", ex.Message);
        }
    }

}
