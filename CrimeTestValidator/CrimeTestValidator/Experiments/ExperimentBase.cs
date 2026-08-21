using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using CrimeTestValidator.Configs;
using CrimeTestValidator.Dtos;
using CrimeTestValidator.Enums;
using CrimeTestValidator.Inference;
using CsvHelper;
using CsvHelper.Configuration;

namespace CrimeTestValidator.Experiments;

public abstract class ExperimentBase : IExperiment
{
    protected readonly ExperimentConfig Config;
    protected readonly IInferenceClient Inference;

    protected ExperimentBase(ExperimentConfig config, IInferenceClient inference)
    {
        Config = config;
        Inference = inference;
    }

    public abstract ExperimentType Type { get; }

    protected virtual int MaxConcurrency => 4;

    protected abstract void Load();

    protected abstract IReadOnlyList<ExperimentTask> BuildTasks();

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"=== {Type} ===");
        Load();

        var tasks = BuildTasks();
        var concurrency = Math.Max(1, MaxConcurrency);
        Console.WriteLine($"{tasks.Count} calls, concurrency {concurrency}.");

        var bag = new ConcurrentBag<(int Order, ExperimentResultDto Result)>();
        var completed = 0;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = concurrency,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(tasks.Select((t, i) => (Task: t, Index: i)), options,
            async (item, ict) =>
            {
                var response = await Inference.AskAsync(item.Task.Request, ict);
                bag.Add((item.Index, Project(item.Task, response)));

                var n = Interlocked.Increment(ref completed);
                if (n % 25 == 0 || n == tasks.Count)
                    Console.WriteLine($"  {n}/{tasks.Count}");
            });

        var ordered = bag.OrderBy(x => x.Order).Select(x => x.Result).ToList();
        var file = $"{Type.ToString().ToLowerInvariant()}-results-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv";
        await WriteResultsAsync(file, ordered, ct);
        Console.WriteLine($"Results saved to: {file}");
    }

    private static ExperimentResultDto Project(ExperimentTask task, InferenceResult result) => new()
    {
        ScenarioId = task.ScenarioId,
        QuestionId = task.QuestionId,
        ExpectedAnswer = task.ExpectedAnswer,
        Result = result.Content,
        LatencyMs = result.LatencyMs,
        Attempts = result.Attempts,
        Error = result.Error
    };

    protected static List<T> ReadCsv<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"CSV not found: {path}", path);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<T>().ToList();
    }

    protected static async Task WriteResultsAsync<T>(string path, IEnumerable<T> rows, CancellationToken ct)
    {
        await using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(rows, ct);
    }
}
