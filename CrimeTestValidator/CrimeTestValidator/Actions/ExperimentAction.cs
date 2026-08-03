using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CrimeTestValidator.Dtos;
using CsvHelper;
using CsvHelper.Configuration;

namespace CrimeTestValidator.Actions;

public class ExperimentAction
{
	private readonly ExperimentConfig _config;
	private List<ScenarioDto> _scenarios;
	private Dictionary<int, List<QuestionDto>> _questions;

	public ExperimentAction(ExperimentConfig config)
	{
		var validationResult = config.Validate();

		if(!validationResult.IsValid)
			throw new ArgumentException(validationResult.Msg);

		_config = config;
		_scenarios = new();
		_questions = new();
		PrepareScenarios();
		PrepareQuestions();
	}

	public async Task RunExperimentsAsync()
	{
		Console.WriteLine("Starting experiments...");
		var resultsBag = new ConcurrentBag<ExperimentResultDto>();
		await Parallel.ForEachAsync(_scenarios, async (scenario, ct) =>
		{
			Console.WriteLine("Processing scenario {0}...", scenario.ScenarioId);
			await Parallel.ForEachAsync(_questions[scenario.ScenarioId], ct, async (q, ict) =>
			{
				Console.WriteLine("Processing question {0}...", q.QuestionId);
				var payload = new
				{
						model = _config.Model,
						messages = new[]
						{
								new
								{
										role = "system",
										content = $"SCENARIO: {scenario.Scenario} \n\n {scenario.Prompt}"
								},
								new { role = "user", content = q.Question }
						}
				};

				var json = JsonSerializer.Serialize(payload);
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				using var client  = new HttpClient();
				client.Timeout = TimeSpan.FromMinutes(5);
				var response = await client.PostAsync(_config.ApiUrl, content, ict);
				var result = new ExperimentResultDto()
				{
						Result = await response.Content.ReadAsStringAsync(ict),
						ScenarioId = scenario.ScenarioId,
						QuestionId = q.QuestionId
				};

				resultsBag.Add(result);
			});
		});

		var resultsFile = $"results-{DateTime.Now:dd-MM-yyyy:hh:mm:ss}.csv";
		await using var writer = new StreamWriter(resultsFile);
		await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
		await csv.WriteRecordsAsync(resultsBag);
		Console.WriteLine($"Finished experiments. Wrote results to: {resultsFile}...");
	}

	private void PrepareScenarios()
	{
		Console.WriteLine("Preparing scenarios...");
		using var reader = new StreamReader(_config.ScenariosFile);
		var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);
		csvConfig.HeaderValidated = null;
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
		_scenarios = csv.GetRecords<ScenarioDto>().ToList();

		for (var i = 0; i < _scenarios.Count; i++)
			_scenarios[i].ScenarioId = i + 1;

		Console.WriteLine("Loaded {0} scenarios", _scenarios.Count);
	}

	private void PrepareQuestions()
	{
		Console.WriteLine("Preparing questions...");
		using var reader = new StreamReader(_config.QuestionsFile);
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
		var questions = csv.GetRecords<QuestionDto>().ToList();
		_questions = questions
				.GroupBy(q => q.ScenarioId, q => q)
				.ToDictionary(g => g.Key, g => g.ToList());
		Console.WriteLine("Loaded {0} questions", _questions.Values.Count);
	}
}
