namespace CrimeTestValidator.Configs;

public class ValidationConfig : IConfig
{
	public string AnswersFile { get; set; } = null!;

	public string SubjectsFile { get; set; } = null!;

	public (bool IsValid, string Msg) Validate()
	{
		var invalidCount = 0;
		var invalidMsg = "";

		if (string.IsNullOrEmpty(AnswersFile) || !File.Exists(AnswersFile))
		{
			invalidCount++;
			invalidMsg += "Answers file is either missing or not provided!\n";
		}

		if (string.IsNullOrEmpty(SubjectsFile) || !File.Exists(SubjectsFile))
		{
			invalidCount++;
			invalidMsg += "Subjects file is either missing or not provided!";
		}

		return (invalidCount == 0, invalidMsg);
	}
}
