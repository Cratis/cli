// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Options for building an exported order offline.
/// </summary>
public sealed class BuildOnceSettings : BuildAgentSettings
{
    /// <summary>
    /// Gets or sets the order file.
    /// </summary>
    [CommandOption("--order <FILE>")]
    [Description("The build order to build, as exported from Studio.")]
    public string Order { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets where to write the outcome.
    /// </summary>
    [CommandOption("--outcome <FILE>")]
    [Description("Write the outcome to this file instead of the standard output.")]
    public string? Outcome { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a commit is pushed.
    /// </summary>
    [CommandOption("--push")]
    [Description("Push the commit to origin with your own Git credentials.")]
    public bool Push { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate() =>
        string.IsNullOrWhiteSpace(Order) ? ValidationResult.Error("Name the order with --order <FILE>.") : base.Validate();
}

/// <summary>
/// Builds an exported order offline, without a Studio connection, and writes the outcome.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "once",
    "Build an exported build order offline with a coding agent or a generator, verify it with the kit's commands and commit it. Writes the outcome document; needs no Studio connection.",
    Branch = typeof(BuildBranch))]
[CliExample("build", "once", "--order", "order.json", "--agent", "claude")]
[CliExample("build", "once", "--order", "order.json", "--exec", "./generate.sh", "--verify", "dotnet test")]
public sealed class BuildOnceCommand : AsyncCommand<BuildOnceSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, BuildOnceSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var order = BuildOrder.Parse(await File.ReadAllTextAsync(settings.Order, cancellationToken));
            var kit = BuildHostFactory.Kit(settings);
            var options = await BuildHostFactory.Create(settings, kit, settings.Push, null, cancellationToken);
            var outcome = await new BuildHost(options).Build(order, BuildGitInstructions.Offline, cancellationToken);
            var json = outcome.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            if (settings.Outcome is { } path) await File.WriteAllTextAsync(path, json + Environment.NewLine, cancellationToken);
            else await Console.Out.WriteLineAsync(json);
            return outcome["kind"]?.GetValue<string>() == BuildOutcomes.Failed ? ExitCodes.ValidationError : ExitCodes.Success;
        }
        catch (Exception exception) when (exception is InvalidBuildOrder or InvalidBuildKit or ArgumentException or IOException)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }
}
