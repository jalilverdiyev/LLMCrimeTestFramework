namespace CrimeTestValidator.Dtos;

public class ValidationSubjectDto
{
	public string Result { get; set; } = null!;

	public int ScenarioId { get; set; }

	public int QuestionId { get; set; }
}
