using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;

namespace DynamicCalculatorAPI.Services
{
    public class CalculateService : ICalculateService
    {
        private readonly IPaymentRepository paymentRepository;
        public CalculateService(IPaymentRepository paymentRepository)
        {
            this.paymentRepository = paymentRepository;    
        }
        public async Task<List<MethodComparisonDto>> GetComparisonReportAsync(Guid jobId)
        {
          return await  paymentRepository.GetComparisonReportAsync(jobId);
        }
    }
}
