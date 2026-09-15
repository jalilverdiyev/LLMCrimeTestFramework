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

		using var subjectsReader = new StreamReader(_config.SubjectsFile);
		using var subjectsCsv = new CsvReader(subjectsReader, CultureInfo.InvariantCulture);
		var subjects = subjectsCsv.GetRecords<ExperimentResultDto>();

		Console.WriteLine("Starting validations...");
		Parallel.ForEach(subjects, (subject, _) =>
		{
			var actual = TextSanitizer.Sanitize(subject.Result);
			var expected = TextSanitizer.Sanitize(subject.ExpectedAnswer ?? string.Empty);
			var isValid = actual.Contains(expected, StringComparison.InvariantCultureIgnoreCase);
			results.Add(new (subject.ScenarioId, subject.QuestionId, isValid));

			if(!isValid)
				failures.Add($"{subject.ScenarioId} : {subject.QuestionId} => {expected} | {actual}\n\n\n");
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
