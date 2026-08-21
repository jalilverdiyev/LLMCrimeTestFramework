using CrimeTestValidator.Enums;

namespace CrimeTestValidator.Experiments;

public interface IExperiment
{
	ExperimentType Type { get; }
	Task RunAsync(CancellationToken ct = default);
}