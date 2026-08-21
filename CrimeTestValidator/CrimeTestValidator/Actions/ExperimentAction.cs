using CrimeTestValidator.Configs;
using CrimeTestValidator.Enums;
using CrimeTestValidator.Experiments;
using CrimeTestValidator.Inference;

namespace CrimeTestValidator.Actions;

public class ExperimentAction
{
	private readonly ExperimentConfig _config;
	private readonly IInferenceClient _inference;

	public ExperimentAction(ExperimentConfig config)
			: this(config, new InferenceClient(config)) { }

	public ExperimentAction(ExperimentConfig config, IInferenceClient inference)
	{
		var validationResult = config.Validate();
		if (!validationResult.IsValid)
			throw new ArgumentException(validationResult.Msg);

		_config = config;
		_inference = inference;
	}

	public Task RunExperimentsAsync(CancellationToken ct = default) =>
			Create(_config.ExperimentType).RunAsync(ct);

	private IExperiment Create(ExperimentType type) => type switch
	{
			ExperimentType.FactualRecall => new FactualRecallExperiment(_config, _inference),
			ExperimentType.TheoryOfMind  => new TheoryOfMindExperiment(_config, _inference),
			ExperimentType.LieAbility    => new LieAbilityExperiment(_config, _inference),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "No experiment configured.")
	};
}
