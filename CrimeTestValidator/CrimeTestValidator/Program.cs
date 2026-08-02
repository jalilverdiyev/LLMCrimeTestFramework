// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using CrimeTestValidator.Actions;
using CrimeTestValidator.Dtos;

if (args.Length == 0 || !int.TryParse(args[0], out int actionNum))
{
	Console.WriteLine("Not enough arguments");
	return;
}

switch ((ActionType)actionNum)
{
	case ActionType.Experiment:
		if (args.Length < 2 || string.IsNullOrEmpty(args[1]))
		{
			Console.WriteLine("Invalid arguments");
			return;
		}

		var modelName = args[1];
		var configJson = File.ReadAllText("config.json");
		Console.WriteLine($"Config: {configJson}");
		var config = JsonSerializer.Deserialize<ExperimentConfig>(configJson,
				new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

		if (config == null)
		{
			Console.WriteLine("Invalid config");
			return;
		}

		config.Model = modelName;
		var experimentAction = new ExperimentAction(config);
		await experimentAction.RunExperimentsAsync();
		break;
	case ActionType.Validate:
		break;
	default:
		Console.WriteLine("Wrong input");
		return;
}
