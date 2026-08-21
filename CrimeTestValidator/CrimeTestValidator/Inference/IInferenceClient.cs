namespace CrimeTestValidator.Inference;

public interface IInferenceClient
{
	Task<InferenceResult> AskAsync(InferenceRequest request, CancellationToken ct);
}