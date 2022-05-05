using Spectre.Console.Cli;

namespace dcma.Remove;

public class RemoveSettings : CommandSettings, IIdentifierSettings
{
    [CommandArgument(0, "[ImageIdentifier]")] 
    public string? ImageIdentifier { get; set; }
}