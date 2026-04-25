using DynamicCalculatorAPI.Models;
using Microsoft.EntityFrameworkCore;
namespace DynamicCalculatorAPI.DBContext
{
    public class PaymentContext:DbContext
    {
        public PaymentContext(DbContextOptions<PaymentContext> options) : base(options)
        {
        }
        public DbSet<TargilModel> Targil { get; set; }
        public DbSet<LogModel> Log { get; set; }
        public DbSet<DataModel> Data { get; set; }
        public DbSet<ResultModel> Result { get; set; }

        public DbSet<JobModel> Job { get; set; }
    }
}
