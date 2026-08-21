namespace CrimeTestValidator.Dtos;

public class ExperimentResultDto
{
	public string Result { get; set; } = null!;

	public int ScenarioId { get; set; }

	public int QuestionId { get; set; }

	public string? ExpectedAnswer { get; set; }

	public int LatencyMs { get; set; }

	public int Attempts { get; set; }

	public string? Error { get; set; }
}
