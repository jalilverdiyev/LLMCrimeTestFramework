namespace CrimeTestValidator.Inference;

public sealed record InferenceRequest(
		string SystemPrompt,
		string UserPrompt,
		int MaxTokens = 512);
