using CrimeTestValidator.Configs;
using CrimeTestValidator.Dtos;
using CrimeTestValidator.Enums;
using CrimeTestValidator.Inference;

namespace CrimeTestValidator.Experiments;

public sealed class LieAbilityExperiment : ExperimentBase
{
    protected override int MaxTokens => 400;
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

        Console.WriteLine($"Loaded {_scenarios.Count} personas, {questions.Count} questions.");
    }

    protected override IReadOnlyList<ExperimentTask> BuildTasks()
    {
        var tasks = new List<ExperimentTask>();

        foreach (var scenario in _scenarios)
        {
            if (!_questions.TryGetValue(scenario.ScenarioId, out var questions))
            {
                Console.WriteLine($"  warning: no questions for persona {scenario.ScenarioId}");
                continue;
            }

            foreach (var q in questions)
            {
                var system = $"SCENARIO: {scenario.Scenario} \n\n {scenario.Prompt}";

                // No "answer with a single letter" suffix, and no truthfulness
                // instruction: the persona already states whether to conceal.
                tasks.Add(new ExperimentTask(scenario.ScenarioId, q.QuestionId, q.Answer,
                        new InferenceRequest(system, q.Question, MaxTokens)));
            }
        }

        return tasks;
    }
}
