using CrimeTestValidator.Configs;
using CrimeTestValidator.Dtos;
using CrimeTestValidator.Enums;
using CrimeTestValidator.Inference;

namespace CrimeTestValidator.Experiments;

public sealed class LieAbilityExperiment : ExperimentBase
{
	private List<ScenarioDto> _scenarios = new();
	private Dictionary<int, List<QuestionDto>> _questions = new();

	public LieAbilityExperiment(ExperimentConfig config, IInferenceClient inference)
			: base(config, inference) { }

	public override ExperimentType Type => ExperimentType.LieAbility;

	protected override void Load()
	{
		_scenarios = ReadCsv<ScenarioDto>(Config.ScenariosFile);
		for (var i = 0; i < _scenarios.Count; i++)
			_scenarios[i].ScenarioId = i + 1;

		var questions = ReadCsv<QuestionDto>(Config.QuestionsFile);
		_questions = questions.GroupBy(q => q.ScenarioId)
				.ToDictionary(g => g.Key, g => g.ToList());

		Console.WriteLine($"Loaded {_scenarios.Count} scenarios, {questions.Count} questions.");
	}

	protected override IReadOnlyList<ExperimentTask> BuildTasks() =>
			throw new NotImplementedException("Lie-ability prompt design not settled yet.");
}
