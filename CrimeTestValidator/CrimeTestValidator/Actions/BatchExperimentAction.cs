using System.IO.Compression;
using CrimeTestValidator.Configs;
using CrimeTestValidator.Enums;

namespace CrimeTestValidator.Actions;

public class BatchExperimentAction
{
	private readonly BatchExperimentConfig _config;

	public BatchExperimentAction(BatchExperimentConfig config)
	{
		var validationResult = config.Validate();
        
		if (!validationResult.IsValid)
			throw new ArgumentException(validationResult.Msg);

		_config = config;
	}

	public async Task RunExperimentsAsync(CancellationToken ct = default)
	{
		var extractionPath = Path.Combine(Path.GetTempPath(), $"CrimeTestValidator-{Guid.NewGuid():N}");
		Directory.CreateDirectory(extractionPath);

		try
		{
			ExtractArchive(extractionPath);
			var experimentDirectories = Directory.EnumerateDirectories(extractionPath, "*", SearchOption.AllDirectories)
				.Where(ContainsExperimentFiles)
				.ToList();

			if (experimentDirectories.Count == 0)
				throw new InvalidDataException("The ZIP file does not contain folders with questions, scenarios, and a .conf file.");

			foreach (var experimentDirectory in experimentDirectories)
			{
				ct.ThrowIfCancellationRequested();
				var experimentConfig = await CreateExperimentConfigAsync(experimentDirectory, ct);
				Console.WriteLine("Running experiment folder: {0}", experimentDirectory);
				await new ExperimentAction(experimentConfig).RunExperimentsAsync(ct);
			}
		}
		finally
		{
			Directory.Delete(extractionPath, recursive: true);
		}
	}

	private void ExtractArchive(string extractionPath)
	{
		using var archive = ZipFile.OpenRead(_config.ZipPath);
		var extractionRoot = Path.GetFullPath(extractionPath) + Path.DirectorySeparatorChar;

		foreach (var entry in archive.Entries)
		{
			var destinationPath = Path.GetFullPath(Path.Combine(extractionPath, entry.FullName));
			if (!destinationPath.StartsWith(extractionRoot, StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("The ZIP file contains an invalid entry path.");

			if (string.IsNullOrEmpty(entry.Name))
			{
				Directory.CreateDirectory(destinationPath);
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
			entry.ExtractToFile(destinationPath);
		}
	}

	private static bool ContainsExperimentFiles(string directory)
	{
		var files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly).ToList();
		return files.Any(file => Path.GetFileName(file).Contains("questions", StringComparison.OrdinalIgnoreCase))
			&& files.Any(file => Path.GetFileName(file).Contains("scenarios", StringComparison.OrdinalIgnoreCase))
			&& files.Count(file => string.Equals(Path.GetExtension(file), ".conf", StringComparison.OrdinalIgnoreCase)) == 1;
	}

	private async Task<ExperimentConfig> CreateExperimentConfigAsync(string experimentDirectory, CancellationToken ct)
	{
		var files = Directory.EnumerateFiles(experimentDirectory, "*", SearchOption.TopDirectoryOnly).ToList();
		var questionsFile = GetSingleFile(files, "questions", experimentDirectory);
		var scenariosFile = GetSingleFile(files, "scenarios", experimentDirectory);
		var confFile = GetSingleFile(files, ".conf", experimentDirectory, file =>
			string.Equals(Path.GetExtension(file), ".conf", StringComparison.OrdinalIgnoreCase));
		var typeText = (await File.ReadAllTextAsync(confFile, ct)).Trim();

		if (!int.TryParse(typeText, out var typeValue) || !Enum.IsDefined(typeof(ExperimentType), typeValue))
			throw new InvalidDataException($"The .conf file must contain a valid experiment type: {confFile}");

		return new ExperimentConfig
		{
			QuestionsFile = questionsFile,
			ScenariosFile = scenariosFile,
			ApiUrl = _config.ApiUrl,
			Model = _config.Model,
			ExperimentType = (ExperimentType)typeValue,
			IsThinkingEnabled = _config.IsThinkingEnabled,
		};
	}

	private static string GetSingleFile(
		IEnumerable<string> files,
		string description,
		string experimentDirectory,
		Func<string, bool>? predicate = null)
	{
		predicate ??= file => Path.GetFileName(file).Contains(description, StringComparison.OrdinalIgnoreCase);
		var matches = files.Where(predicate).ToList();

		if (matches.Count != 1)
			throw new InvalidDataException($"Expected one {description} file in: {experimentDirectory}");

		return matches[0];
	}
}