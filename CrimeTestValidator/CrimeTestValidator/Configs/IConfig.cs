namespace CrimeTestValidator.Configs;

public interface IConfig
{
	(bool IsValid, string Msg) Validate();
}
