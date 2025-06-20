using Spectre.Console.Cli;

namespace Migrator.Cli;

/// <summary>
/// Общие параметры командной строки для всех подкоманд.
/// </summary>
public class CommonSettings : CommandSettings
{
    [CommandOption("-c|--config <FILE>")]
    public string ConfigPath { get; init; } = "config.yaml";

    [CommandOption("--dry-run")]
    public bool DryRun { get; init; }

    [CommandOption("-v|--verbose")]
    public bool Verbose { get; init; }
}
