namespace CrimeTestValidator.Inference;

public sealed record InferenceResult(
		string Content,
		bool Success,
		string? Error,
		int LatencyMs,
		int Attempts)
{
	public static InferenceResult Failed(string error, int latencyMs, int attempts) =>
			new(string.Empty, false, error, latencyMs, attempts);
}