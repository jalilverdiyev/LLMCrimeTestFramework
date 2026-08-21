using CrimeTestValidator.Actions;
using CrimeTestValidator.Enums;

namespace CrimeTestValidator.Configs;

public class ExperimentConfig : IConfig
{
	public string ScenariosFile { get; set; } = null!;

	public string QuestionsFile { get; set; } = null!;

	public string ApiUrl { get; set; } = null!;

	public string Model { get; set; } = null!;

	public ExperimentType ExperimentType { get; set; }

	public (bool IsValid, string Msg) Validate()
	{
		var invalidCount = 0;
		var invalidMsg = "";

		if (!File.Exists(ScenariosFile) || !File.Exists(QuestionsFile))
		{
			invalidCount++;
			invalidMsg += "The scenarios and questions files don't exist\n";
		}

		if(string.IsNullOrEmpty(ApiUrl))
		{
			invalidCount++;
			invalidMsg += "ApiUrl is missing\n";
		}

		if(string.IsNullOrEmpty(Model))
		{
			invalidCount++;
			invalidMsg += "Model is missing";
		}

		if (ExperimentType == ExperimentType.None)
		{
			invalidCount++;
			invalidMsg += "ExperimentType is missing";
		}

		return (invalidCount == 0,  invalidMsg);
	}
}
