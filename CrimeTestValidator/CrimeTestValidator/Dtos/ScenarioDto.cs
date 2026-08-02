using CsvHelper.Configuration.Attributes;

namespace CrimeTestValidator.Dtos;

public class ScenarioDto
{
	[Ignore]
	public int ScenarioId { get; set; }

	public string Scenario { get; set; } = null!;

	public string Prompt { get; set; } = null!;
}
