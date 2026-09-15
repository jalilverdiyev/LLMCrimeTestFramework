namespace CrimeTestValidator.Configs;

public class BatchExperimentConfig : IConfig
{
	public string ZipPath { get; set; } = null!;

	public string ApiUrl { get; set; } = null!;

	public string Model { get; set; } = null!;

	public bool IsThinkingEnabled { get; set; }

	public (bool IsValid, string Msg) Validate()
	{
		if (string.IsNullOrWhiteSpace(ZipPath))
			return (false, "ZipPath is missing");

		if (!File.Exists(ZipPath))
			return (false, "The ZIP file doesn't exist");

		if (!string.Equals(Path.GetExtension(ZipPath), ".zip", StringComparison.OrdinalIgnoreCase))
			return (false, "ZipPath must point to a .zip file");

		if (string.IsNullOrWhiteSpace(ApiUrl))
			return (false, "ApiUrl is missing");

		if (string.IsNullOrWhiteSpace(Model))
			return (false, "Model is missing");

		return (true, string.Empty);
	}
}