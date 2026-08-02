namespace CrimeTestValidator.Dtos;

public class ExperimentResultDto
{
	public string Result { get; set; } = null!;

	public int ScenarioId { get; set; }

	public int QuestionId { get; set; }
}
