// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using CrimeTestValidator.Actions;
using CrimeTestValidator.Configs;

JsonSerializerOptions jsonOptions = new()
{
		PropertyNameCaseInsensitive = true,
};

if (args.Length == 0 || !int.TryParse(args[0], out int actionNum))
{
	Console.WriteLine("Not enough arguments");
	return;
}

var configJson = File.ReadAllText("config.json");
Console.WriteLine($"Config: {configJson}");
var appConfig = JsonSerializer.Deserialize<AppConfig>(configJson, jsonOptions);

if (appConfig == null)
{
	Console.WriteLine("Config is invalid or missing");
	return;
}

switch ((ActionType)actionNum)
{
	case ActionType.Experiment:
	{
		if(args.Length <= 2)
			throw new ArgumentException("Model is mising");

		appConfig.ExperimentConfig.Model = args[1];
		var experimentAction = new ExperimentAction(appConfig.ExperimentConfig);
		await experimentAction.RunExperimentsAsync();
		break;
	}
	case ActionType.Validate:
	{
		var validationAction = new ValidationAction(appConfig.ValidationConfig);
		await validationAction.RunValidationsAsync();
		break;
	}
	default:
		Console.WriteLine("There isn't such action");
		return;
}
