namespace CrimeTestValidator.Dtos;

public class QuestionDto
{
	public int ScenarioId { get; set; }

	public int QuestionId { get; set; }

	public string Question { get; set; } = null!;

	public string Answer { get; set; } = null!;
}
