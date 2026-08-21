using CrimeTestValidator.Inference;

namespace CrimeTestValidator.Experiments;

public sealed record ExperimentTask(
		int ScenarioId,
		int QuestionId,
		string? ExpectedAnswer,
		InferenceRequest Request);
