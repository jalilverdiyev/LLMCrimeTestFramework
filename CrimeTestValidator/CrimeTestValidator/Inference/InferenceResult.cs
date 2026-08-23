namespace CrimeTestValidator.Inference;

public sealed record InferenceResult(
		string Content,
		string? Thinking,
		bool Success,
		string? Error,
		int LatencyMs,
		int Attempts)
{
	public static InferenceResult Failed(string error, int latencyMs, int attempts) =>
			new(string.Empty, null, false, error, latencyMs, attempts);
}
