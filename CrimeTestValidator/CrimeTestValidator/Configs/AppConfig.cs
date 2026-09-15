namespace CrimeTestValidator.Configs;

public class AppConfig
{
	public ExperimentConfig ExperimentConfig { get; set; } = null!;

	public ValidationConfig ValidationConfig { get; set; } = null!;

	public BatchExperimentConfig BatchExperimentConfig { get; set; } = null!;
}
