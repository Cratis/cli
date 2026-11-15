// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Build;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.Commands.Studio;

/// <summary>
/// Options for fetching context from Studio.
/// </summary>
public sealed class StudioFetchSettings : GlobalSettings
{
    /// <summary>The scopes a context can be fetched for.</summary>
    public static readonly IReadOnlyList<string> Scopes = ["slice", "feature", "eventModel", "domain"];

    /// <summary>The formats a context can be fetched in; <c language="csharp">text</c> is Markdown.</summary>
    public static readonly IReadOnlyList<string> Formats = ["markdown", "text", "json", "yaml", "toon"];

    /// <summary>
    /// Gets or sets the scope.
    /// </summary>
    [CommandOption("--context <SCOPE>")]
    [Description("What to fetch the context of: slice, feature, eventModel or domain.")]
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [CommandOption("--id <ID>")]
    [Description("The identifier of the slice, feature, event model or domain.")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format.
    /// </summary>
    [CommandOption("--format <FORMAT>")]
    [Description("markdown (or text), json, yaml or toon. Defaults to markdown. TOON fits a large context into a token budget.")]
    public string Format { get; set; } = "markdown";

    /// <summary>
    /// Gets or sets the file to write to.
    /// </summary>
    [CommandOption("--out <PATH>")]
    [Description("Write the context to this file instead of the standard output.")]
    public string? Out { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Screenplay guidance is left out.
    /// </summary>
    [CommandOption("--no-guidance")]
    [Description("Leave out the Screenplay guidance, to save tokens when the agent already knows Screenplay.")]
    public bool NoGuidance { get; set; }

    /// <summary>
    /// Gets the scope by the name Studio's export tool takes.
    /// </summary>
    internal string Scope => Scopes.First(scope => string.Equals(scope, Context.Replace("-", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the format by the name Studio's export tool takes.
    /// </summary>
    internal string ExportFormat => string.Equals(Format, "text", StringComparison.OrdinalIgnoreCase) ? "markdown" : Format.ToLowerInvariant();

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (!Scopes.Any(scope => string.Equals(scope, Context.Replace("-", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Error($"Name what to fetch with --context: {string.Join(", ", Scopes)}.");
        }

        if (!Guid.TryParse(Id, out _)) return ValidationResult.Error("Name its identifier with --id <id>.");
        if (!Formats.Contains(Format.ToLowerInvariant())) return ValidationResult.Error($"--format must be one of {string.Join(", ", Formats)}.");
        return base.Validate();
    }
}

/// <summary>
/// Fetches the context of a slice, a feature, an event model or a domain from Studio, in a form made for coding agents.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand(
    "fetch",
    "Fetch the context of a slice, a feature, an event model or a domain from Studio for a coding agent or a pipeline: its commands, events, read models, specifications and Screenplay, and the Screenplay guidance unless left out. Uses the organization's MCP credential configured with 'cratis build connect'. Changes nothing.",
    Branch = typeof(StudioBranch))]
[CliExample("studio", "fetch", "--context", "slice", "--id", "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f")]
[CliExample("studio", "fetch", "--context", "feature", "--id", "4a7e2c91-5d3b-4e8f-a1c2-9b8d7e6f5a40", "--format", "toon", "--out", "context.toon")]
public sealed class StudioFetchCommand : AsyncCommand<StudioFetchSettings>
{
    /// <summary>
    /// Fetches a context.
    /// </summary>
    /// <param name="call">Calls a Studio MCP tool.</param>
    /// <param name="settings">The options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The context's text.</returns>
    /// <exception cref="StudioMcpError">Studio did not answer with a context.</exception>
    public static async Task<string> Fetch(Func<string, JsonObject, CancellationToken, Task<JsonNode?>> call, StudioFetchSettings settings, CancellationToken cancellationToken)
    {
        var arguments = new JsonObject { ["scope"] = settings.Scope, ["id"] = settings.Id, ["format"] = settings.ExportFormat, ["includeGuidance"] = !settings.NoGuidance };
        var answer = await call("studio_export_context", arguments, cancellationToken);
        if (answer?["answered"] is JsonValue answered && answered.TryGetValue<bool>(out var ok) && !ok)
        {
            throw new StudioMcpError(answer["note"]?.ToString() ?? "Studio did not answer with a context.");
        }

        return answer?["context"]?["content"]?.GetValue<string>() ?? throw new StudioMcpError(answer?["note"]?.ToString() ?? "Studio answered without a context.");
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, StudioFetchSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var connection = await StudioConnection.Open(cancellationToken);
            if (connection.Mcp is not { } mcp) throw new StudioNotConnected("Fetching context needs the organization's MCP credential. Run 'cratis build connect --mcp-client-id <id>' first.");
            var content = await Fetch(mcp.Call, settings, cancellationToken);
            if (settings.Out is { } path) await File.WriteAllTextAsync(path, content, cancellationToken);
            else await Console.Out.WriteAsync(content);
            return ExitCodes.Success;
        }
        catch (StudioNotConnected exception)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (StudioMcpError exception)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }
        catch (Exception exception) when (exception is HttpRequestException or DirectAuthError)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }
    }
}
