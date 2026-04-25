using Dapper;
using DynamicCalculatorAPI.DBContext;
using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DynamicCalculatorAPI.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IDbContextFactory<PaymentContext> _contextFactory;

        public PaymentRepository(IDbContextFactory<PaymentContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // ─── שליפת נוסחאות ───────────────────────────────────────────
        public async Task<int> GetTotalCountAsync()
        {
            const string sql = "SELECT COUNT(*) FROM t_data";

            using var context = _contextFactory.CreateDbContext();
            using var conn = new SqlConnection(context.Database.GetConnectionString());
            return await conn.ExecuteScalarAsync<int>(sql);
        }

        public async Task<List<DataModel>> GetDataPageAsync(int pageNumber, int pageSize)
        {
            int skip = pageNumber * pageSize;

            const string sql = @"
        SELECT data_id, a, b, c, d
        FROM t_data
        ORDER BY data_id
        OFFSET @skip ROWS
        FETCH NEXT @take ROWS ONLY;
    ";

            using var context = _contextFactory.CreateDbContext();
            using var conn = new SqlConnection(context.Database.GetConnectionString());
            var result = await conn.QueryAsync<DataModel>(sql, new
            {
                skip,
                take = pageSize
            });

            return result.ToList();
        }


        public async Task<IEnumerable<TargilModel>> GetAllTargilsAsync()
        {
            using var context = _contextFactory.CreateDbContext();

            return await context.Targil
                .AsNoTracking()             
                .OrderBy(t => t.targil_id)
                .ToListAsync();
        }

        public async Task<List<DataModel>> GetTopXAsync(int count)
        {
            const string sql = @"
        SELECT TOP (@count) data_id, a, b, c, d
        FROM t_data
        ORDER BY data_id;
    ";

            using var context = _contextFactory.CreateDbContext();
            using var conn = new SqlConnection(context.Database.GetConnectionString());
            var result = await conn.QueryAsync<DataModel>(sql, new { count });
            return result.ToList();
        }


        // ─── שמירת תוצאות בבאצ' ──────────────────────────────────────

        public async Task SaveResultsBatchAsync(IEnumerable<ResultModel> results)
        {
            using var context = _contextFactory.CreateDbContext();

            await context.Result.AddRangeAsync(results);
            await context.SaveChangesAsync();
        }

        // ─── שמירת לוג ───────────────────────────────────────────────

        public async Task SaveLogAsync(LogModel log)
        {
            using var context = _contextFactory.CreateDbContext();

            context.Log.Add(log);
            await context.SaveChangesAsync();
        }
        //----ביצוע הלוגיקה באמצעות פרוצדורה----------------------------------
        public async Task CalculateDynamicAsync(Guid jobId, int? limit = null)
        {
            using var context = _contextFactory.CreateDbContext();
            using var connection = context.Database.GetDbConnection();

            var result = await connection.QueryAsync(
                "sp_CalculateDynamic",
                new { job_id = jobId, limitCount = limit },        
                commandType: CommandType.StoredProcedure
            );
        }
        //----חישוב זמני ריצה של השיטות השונות-------------------------
        public async Task<List<MethodComparisonDto>> GetComparisonReportAsync(Guid jobId)
        {
            using var context = _contextFactory.CreateDbContext();
            using var connection = context.Database.GetDbConnection();

            var result = await connection.QueryAsync<MethodComparisonDto>(
                "sp_GetComparisonReport",
                new { job_id = jobId },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

    }
}
