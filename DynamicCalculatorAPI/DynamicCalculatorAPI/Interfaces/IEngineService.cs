namespace DynamicCalculatorAPI.Interfaces
{
    public interface IEngineService
    {
        Task RunAsync(Guid jobId,int? limit = null);
    }
}
