using DynamicCalculatorAPI.Models;
using System.Data;
using System.Formats.Tar;

namespace DynamicCalculatorAPI.Interfaces
{
    public interface IMethodEngine
    {
        string Name { get; }
        Task RunAsync(Guid jobId, int? limit = null);
    }


}
