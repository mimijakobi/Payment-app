using DynamicCalculatorAPI.Models;
using System.Formats.Tar;

namespace DynamicCalculatorAPI.Interfaces
{
    public interface IPaymentRepository
    {

        Task<int> GetTotalCountAsync();
        Task<List<DataModel>> GetDataPageAsync(int pageNumber, int pageSize);
        Task<IEnumerable<TargilModel>> GetAllTargilsAsync();
        Task SaveResultsBatchAsync(IEnumerable<ResultModel> results);
        Task SaveLogAsync(LogModel log);
        Task CalculateDynamicAsync(Guid jobId, int? limit = null);
        Task<List<MethodComparisonDto>> GetComparisonReportAsync(Guid jobId);
        Task<List<DataModel>> GetTopXAsync(int count);



    }
}
