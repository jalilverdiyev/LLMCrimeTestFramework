namespace CrimeTestValidator.Dtos;

public record ValidationResultDto(int ScenarioId, int QuestionId, bool IsValid);
