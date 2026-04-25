using DynamicCalculatorAPI.Interfaces;

namespace DynamicCalculatorAPI.Services
{
    public class SQLDynamicService : IMethodEngine
    {
        public string Name => "SQL_DB";

        private readonly IPaymentRepository paymentRepository;

        public SQLDynamicService(IPaymentRepository paymentRepository)
        {
                this.paymentRepository = paymentRepository;
        }
        public async Task RunAsync(Guid jobId, int? limit = null)
        {
           await paymentRepository.CalculateDynamicAsync(jobId, limit);
        }
    }
}
