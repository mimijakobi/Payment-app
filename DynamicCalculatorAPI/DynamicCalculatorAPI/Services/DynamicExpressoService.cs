using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;
using DynamicExpresso;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DynamicPaymentCalc.Services
{
   
    public class DynamicExpressoService : IMethodEngine
    {

        private const int PageSize = 50_000;

        
        private const int WriteBatchSize = 10_000;

        
        private const int MaxFormulaParallelism = 3;

        
        private static readonly Regex NormalizeAssignment =
            new(@"(?<![=!<>])=(?![=])", RegexOptions.Compiled);

        private readonly IPaymentRepository _paymentRepository;
        private readonly Interpreter _interpreter;

        public string Name => "DynamicExpresso";

        public DynamicExpressoService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
            _interpreter = BuildInterpreter();
        }

        public async Task RunAsync(Guid jobId, int? limit = null)
        {
            var targils = (await _paymentRepository.GetAllTargilsAsync()).ToList();
            Console.WriteLine($"[INFO] נמצאו {targils.Count} נוסחאות");

            IReadOnlyList<DataModel> allData;

            if (limit.HasValue)
            {
                Console.WriteLine($"[INFO] מצב הדגמה — טוען {limit.Value} רשומות בלבד");
                allData = await _paymentRepository.GetTopXAsync(limit.Value);
            }
            else
            {
                Console.WriteLine("[INFO] טוען נתונים מלאים...");
                allData = await LoadAllDataAsync();
            }

            Console.WriteLine($"[INFO] נטענו {allData.Count:N0} רשומות");

            var semaphore = new SemaphoreSlim(MaxFormulaParallelism);
            var tasks = targils.Select(t => ProcessTargilAsync(t, allData, jobId, semaphore));
            await Task.WhenAll(tasks);

            Console.WriteLine("[DONE] כל הנוסחאות עובדו");
        }

        private async Task ProcessTargilAsync(
            TargilModel targil,
            IReadOnlyList<DataModel> allData,
            Guid jobId,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                Console.WriteLine($"[START] נוסחה {targil.targil_id}: {targil.targil}");

                
                var calculate = Compile(targil);

                var stopwatch = Stopwatch.StartNew();

                
                var results = ComputeAllParallel(targil, calculate, jobId, allData);

                
                await WriteBatchedAsync(results);

                stopwatch.Stop();

                await _paymentRepository.SaveLogAsync(new LogModel
                {
                    job_id = jobId,
                    targil_id = targil.targil_id,
                    method = Name,
                    run_time = (float)stopwatch.Elapsed.TotalSeconds
                });

                Console.WriteLine($"[DONE] נוסחה {targil.targil_id} — {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            finally
            {
                semaphore.Release();
            }
        }
        private ConcurrentBag<ResultModel> ComputeAllParallel(
            TargilModel targil,
            Func<DataModel, double> calculate,
            Guid jobId,
            IReadOnlyList<DataModel> allData)
        {
            var results = new ConcurrentBag<ResultModel>();

            Parallel.ForEach(
                allData,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                record =>
                {
                    double value = calculate(record);

                    results.Add(new ResultModel
                    {
                        job_id = jobId,
                        data_id = record.data_id,
                        targil_id = targil.targil_id,
                        method = Name,
                        result = (float)value
                    });
                });

            return results;
        }

        private async Task WriteBatchedAsync(IEnumerable<ResultModel> results)
        {
            var chunk = new List<ResultModel>(WriteBatchSize);

            foreach (var item in results)
            {
                chunk.Add(item);

                if (chunk.Count >= WriteBatchSize)
                {
                    await _paymentRepository.SaveResultsBatchAsync(chunk);
                    chunk.Clear();
                }
            }

            if (chunk.Count > 0)
                await _paymentRepository.SaveResultsBatchAsync(chunk);
        }

        private async Task<List<DataModel>> LoadAllDataAsync()
        {
            int total = await _paymentRepository.GetTotalCountAsync();
            int totalPages = (int)Math.Ceiling(total / (double)PageSize);
            var allData = new List<DataModel>(total);

            for (int page = 0; page < totalPages; page++)
            {
                var dataPage = await _paymentRepository.GetDataPageAsync(page, PageSize);
                allData.AddRange(dataPage);
            }

            return allData;
        }

        public Func<DataModel, double> Compile(TargilModel targil)
        {
            
            var parameters = new[]
            {
                new Parameter("a", typeof(double)),
                new Parameter("b", typeof(double)),
                new Parameter("c", typeof(double)),
                new Parameter("d", typeof(double))
            };

            var exprTrue = _interpreter.Parse(targil.targil, parameters);
            var exprFalse = _interpreter.Parse(targil.targil_false ?? "0.0", parameters);

            bool hasCondition = !string.IsNullOrWhiteSpace(targil.tnai);

            if (hasCondition)
            {
                var condExpr = _interpreter.Parse(
                    NormalizeCondition(targil.tnai!), parameters);

                return r =>
                {
                    var args = new object[] { (double)r.A, (double)r.B, (double)r.C, (double)r.D };
                    bool cond = (bool)condExpr.Invoke(args);
                    return cond
                        ? Convert.ToDouble(exprTrue.Invoke(args))
                        : Convert.ToDouble(exprFalse.Invoke(args));
                };
            }

            return r =>
            {
                var args = new object[] { (double)r.A, (double)r.B, (double)r.C, (double)r.D };
                return Convert.ToDouble(exprTrue.Invoke(args));
            };
        }

        private static Interpreter BuildInterpreter()
        {
            var interpreter = new Interpreter().Reference(typeof(Math));

            interpreter.SetFunction("SQRT", (Func<double, double>)Math.Sqrt);
            interpreter.SetFunction("ABS", (Func<double, double>)Math.Abs);
            interpreter.SetFunction("LOG", (Func<double, double>)Math.Log);
            interpreter.SetFunction("POW", (Func<double, double, double>)Math.Pow);
            interpreter.SetFunction("SIN", (Func<double, double>)Math.Sin);
            interpreter.SetFunction("COS", (Func<double, double>)Math.Cos);
            interpreter.SetFunction("TAN", (Func<double, double>)Math.Tan);
            interpreter.SetFunction("CEIL", (Func<double, double>)Math.Ceiling);
            interpreter.SetFunction("FLOOR", (Func<double, double>)Math.Floor);
            interpreter.SetFunction("ROUND", (Func<double, double>)Math.Round);
            interpreter.SetFunction("MIN", (Func<double, double, double>)Math.Min);
            interpreter.SetFunction("MAX", (Func<double, double, double>)Math.Max);

            return interpreter;
        }

        private static string NormalizeCondition(string condition) =>
            NormalizeAssignment.Replace(condition, "==");
    }
}