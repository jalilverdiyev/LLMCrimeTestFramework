namespace CrimeTestValidator.Configs;

public class ValidationConfig : IConfig
{
	public string SubjectsFile { get; set; } = null!;

	public (bool IsValid, string Msg) Validate()
	{
		var invalidCount = 0;
		var invalidMsg = "";

		if (string.IsNullOrEmpty(SubjectsFile) || !File.Exists(SubjectsFile))
		{
			invalidCount++;
			invalidMsg += "Subjects file is either missing or not provided!";
		}

		return (invalidCount == 0, invalidMsg);
	}
}
