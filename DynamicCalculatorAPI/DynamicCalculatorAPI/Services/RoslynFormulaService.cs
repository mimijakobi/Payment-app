using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DynamicPaymentCalc.Services
{

    public class RoslynFormulaService : IMethodEngine
    {
        private const int PageSize = 50_000;

        private const int WriteBatchSize = 10_000;


        private const int MaxFormulaParallelism = 3;

        private static readonly Regex NormalizeAssignment =
            new(@"(?<![=!<>])=(?![=])", RegexOptions.Compiled);


        private static readonly ScriptOptions RoslynOptions = ScriptOptions.Default
            .WithImports("System", "System.Math")
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithAllowUnsafe(false);

        private readonly IPaymentRepository _paymentRepository;

        public string Name => "RoslynFormula";



        public RoslynFormulaService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task RunAsync(Guid jobId, int? limit = null)
        {
            var targils = (await _paymentRepository.GetAllTargilsAsync()).ToList();
            Console.WriteLine($"[INFO] נמצאו {targils.Count} נוסחאות");

            Console.WriteLine("[INFO] מקמפל נוסחאות...");
            var compiled = await CompileAllAsync(targils);
            Console.WriteLine("[INFO] קימפול הושלם");

            IReadOnlyList<DataModel> allData;

            if (limit.HasValue)
            {
                Console.WriteLine($"[INFO] מצב הדגמה — טוען {limit.Value:N0} רשומות בלבד...");
                allData = await _paymentRepository.GetTopXAsync(limit.Value);
            }
            else
            {
                Console.WriteLine("[INFO] טוען נתונים מלאים...");
                allData = await LoadAllDataAsync();
            }

            Console.WriteLine($"[INFO] נטענו {allData.Count:N0} רשומות");

            var semaphore = new SemaphoreSlim(MaxFormulaParallelism);

            var tasks = targils.Select(targil =>
                ProcessTargilAsync(
                    targil,
                    compiled[targil.targil_id],
                    allData,
                    jobId,
                    semaphore));

            await Task.WhenAll(tasks);

            Console.WriteLine("[DONE] כל הנוסחאות עובדו");
        }


        private static async Task<Dictionary<int, Func<FormulaGlobals, double>>> CompileAllAsync(
            IEnumerable<TargilModel> targils)
        {
            var compileTasks = targils.Select(async t =>
            {
                var fn = await CompileAsync(t);
                return (t.targil_id, fn);
            });

            var results = await Task.WhenAll(compileTasks);
            return results.ToDictionary(r => r.targil_id, r => r.fn);
        }

        private async Task ProcessTargilAsync(
            TargilModel targil,
            Func<FormulaGlobals, double> calculate,
            IReadOnlyList<DataModel> allData,
            Guid jobId,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                Console.WriteLine($"[START] נוסחה {targil.targil_id}: {targil.targil}");
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


        private static ConcurrentBag<ResultModel> ComputeAllParallel(
            TargilModel targil,
            Func<FormulaGlobals, double> calculate,
            Guid jobId,
            IReadOnlyList<DataModel> allData)
        {
            var results = new ConcurrentBag<ResultModel>();

            Parallel.ForEach(
                allData,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                record =>
                {

                    var globals = new FormulaGlobals
                    {
                        a = record.A,
                        b = record.B,
                        c = record.C,
                        d = record.D
                    };

                    double value = calculate(globals); // ← סינכרוני, ללא .Result!

                    results.Add(new ResultModel
                    {
                        job_id = jobId,
                        data_id = record.data_id,
                        targil_id = targil.targil_id,
                        method = "RoslynFormula",
                        result = (float)value
                    });
                });

            return results;
        }

        private static async Task<Func<FormulaGlobals, double>> CompileAsync(TargilModel targil)
        {
            bool hasCondition = !string.IsNullOrWhiteSpace(targil.tnai);

            if (hasCondition)
            {

                var (condRunner, trueRunner, falseRunner) = await CompileConditionalAsync(targil);


                return globals =>
                {
                    bool cond = condRunner(globals).GetAwaiter().GetResult();
                    return cond
                        ? trueRunner(globals).GetAwaiter().GetResult()
                        : falseRunner(globals).GetAwaiter().GetResult();
                };
            }
            var mainScript = CSharpScript.Create<double>(
                ConvertFormula(targil.targil),
                RoslynOptions,
                typeof(FormulaGlobals));

            mainScript.Compile();
            var mainRunner = mainScript.CreateDelegate();

            return globals => mainRunner(globals).GetAwaiter().GetResult();
        }

        private static async Task<(
            ScriptRunner<bool> condRunner,
            ScriptRunner<double> trueRunner,
            ScriptRunner<double> falseRunner)>
            CompileConditionalAsync(TargilModel targil)
        {
            var condScript = CSharpScript.Create<bool>(
                NormalizeAssignment.Replace(targil.tnai!, "=="),
                RoslynOptions,
                typeof(FormulaGlobals));

            var trueScript = CSharpScript.Create<double>(
                ConvertFormula(targil.targil),
                RoslynOptions,
                typeof(FormulaGlobals));

            var falseScript = CSharpScript.Create<double>(
                ConvertFormula(targil.targil_false ?? "0.0"),
                RoslynOptions,
                typeof(FormulaGlobals));

            await Task.WhenAll(
                Task.Run(() => condScript.Compile()),
                Task.Run(() => trueScript.Compile()),
                Task.Run(() => falseScript.Compile()));

            return (
                condScript.CreateDelegate(),
                trueScript.CreateDelegate(),
                falseScript.CreateDelegate());
        }

        private static string ConvertFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return "0.0";

            var result = formula;

            // x^y → Math.Pow(x, y)
            result = Regex.Replace(
                result,
                @"(\w+)\^(\w+)",
                m => $"Math.Pow({m.Groups[1].Value}, {m.Groups[2].Value})");

            // פונקציות מתמטיות → Math.<Function>
            result = Regex.Replace(
                result,
                @"\b(sqrt|abs|log|sin|cos|tan|ceil|floor|round|min|max)\s*\(",
                m =>
                {
                    var name = m.Groups[1].Value.ToLower();   // sqrt
                    var pascal = char.ToUpper(name[0]) + name.Substring(1); // Sqrt
                    return $"Math.{pascal}(";
                },
                RegexOptions.IgnoreCase);

            return result;
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


        public class FormulaGlobals
        {
            public double a;
            public double b;
            public double c;
            public double d;

            public double SQRT(double x) => Math.Sqrt(x);
            public double ABS(double x) => Math.Abs(x);
            public double LOG(double x) => Math.Log(x);
            public double POW(double x, double y) => Math.Pow(x, y);
            public double SIN(double x) => Math.Sin(x);
            public double COS(double x) => Math.Cos(x);
            public double TAN(double x) => Math.Tan(x);
            public double CEIL(double x) => Math.Ceiling(x);
            public double FLOOR(double x) => Math.Floor(x);
            public double ROUND(double x) => Math.Round(x);
            public double MIN(double x, double y) => Math.Min(x, y);
            public double MAX(double x, double y) => Math.Max(x, y);

        }
    }
}