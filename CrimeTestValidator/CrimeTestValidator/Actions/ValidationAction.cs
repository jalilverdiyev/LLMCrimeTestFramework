using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text.RegularExpressions;
using CrimeTestValidator.Configs;
using CrimeTestValidator.Dtos;
using CrimeTestValidator.Helpers;
using CsvHelper;
using Newtonsoft.Json.Linq;

namespace CrimeTestValidator.Actions;

public class ValidationAction
{
	private readonly ValidationConfig _config;

	public ValidationAction(ValidationConfig config)
	{
		var validationResult = config.Validate();

		if (!validationResult.IsValid)
			throw new ArgumentException(validationResult.Msg);

		_config = config;
	}

	public async Task RunValidationsAsync()
	{
		var results = new ConcurrentBag<ValidationResultDto>();
		var failures = new ConcurrentBag<string> { "Expected | Actual \n" };
		using var answersReader = new StreamReader(_config.AnswersFile);
		using var answersCsv = new CsvReader(answersReader, CultureInfo.InvariantCulture);
		var answers = answersCsv
				.GetRecords<QuestionDto>()
				.GroupBy(q => q.ScenarioId)
				.ToDictionary(g => g.Key, g=> g.ToList());

		using var subjectsReader = new StreamReader(_config.SubjectsFile);
		using var subjectsCsv = new CsvReader(subjectsReader, CultureInfo.InvariantCulture);
		var subjects = subjectsCsv.GetRecords<ValidationSubjectDto>();

		Console.WriteLine("Starting validations...");
		Parallel.ForEach(subjects, (subject, _) =>
		{
			if(!answers.TryGetValue(subject.ScenarioId, out var scenario))
			{
				Console.WriteLine("Couldn't find scenario with id {0}", subject.ScenarioId);
				results.Add(new(subject.ScenarioId, subject.QuestionId, false));
				return;
			}

			var question = scenario.FirstOrDefault(s => s.QuestionId == subject.QuestionId);

			if (question == null)
			{
				Console.WriteLine("Couldn't find question with id {0}", subject.QuestionId);
				results.Add(new(subject.ScenarioId, subject.QuestionId, false));
				return;
			}

			var msg = JObject.Parse(subject.Result)["message"]?["content"]?.Value<string>() ?? string.Empty;
			var actual = TextSanitizer.Sanitize(msg);
			var expected = TextSanitizer.Sanitize(question.Answer);
			var isValid = actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
			results.Add(new (subject.ScenarioId, subject.QuestionId, isValid));

			if(!isValid)
				failures.Add($"{subject.ScenarioId} : {subject.QuestionId} \n {expected} | {actual}\n\n\n");
		});

		var resultsFile = $"validation_results-{DateTime.Now:dd-MM-yyyy-hh-mm-ss}.csv";
		var failuresFile = $"failures-{DateTime.Now:dd-MM-yyyy-hh-mm-ss}.txt";
		await using var resultsWriter = new StreamWriter(resultsFile);
		await using var resultsCsv = new CsvWriter(resultsWriter, CultureInfo.InvariantCulture);
		await resultsCsv.WriteRecordsAsync(results.OrderBy(r => r.ScenarioId).ThenBy(r => r.QuestionId));
		await File.WriteAllLinesAsync(failuresFile, failures.Reverse());
		Console.WriteLine("Finished validations. Results are saved to {0}...", resultsFile);
		Console.WriteLine($"There were {failures.Count} validation errors. They are saved to {failuresFile}...");
	}
}
