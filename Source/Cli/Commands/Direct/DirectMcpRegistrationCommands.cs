// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Options shared by the Direct MCP registration commands.</summary>
public class DirectMcpRegistrationSettings : GlobalSettings
{
    /// <summary>Gets or sets the clients to act on.</summary>
    [CommandOption("--client <CLIENT>")]
    [Description("MCP clients, comma-separated: claude, codex, copilot, cursor, opencode or pi")]
    public string? Clients { get; set; }

    /// <summary>Gets or sets the scope.</summary>
    [CommandOption("--scope <SCOPE>")]
    [Description("user (your own client configuration, the default) or project (the current directory's)")]
    public string Scope { get; set; } = "user";

    internal IReadOnlyList<string> SelectedClients =>
        Clients?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    internal DirectMcpScope SelectedScope => Scope.ToLowerInvariant() switch
    {
        "user" => DirectMcpScope.User,
        "project" => DirectMcpScope.Project,
        _ => throw new AiMcpConfigurationInvalid("--scope must be 'user' or 'project'.")
    };
}

/// <summary>Options for the Direct MCP registration commands that change client configuration.</summary>
public class DirectMcpChangeSettings : DirectMcpRegistrationSettings
{
    /// <summary>Gets or sets whether to only report the changes.</summary>
    [CommandOption("--dry-run")]
    [Description("Show the exact configuration changes without writing anything")]
    public bool DryRun { get; set; }
}

/// <summary>Options for registering the Direct MCP bridge.</summary>
public sealed class DirectMcpInstallSettings : DirectMcpChangeSettings
{
    /// <summary>Gets or sets the Direct origin the registration pins.</summary>
    [CommandOption("--url <ORIGIN>")]
    [Description("Direct origin to pin in the registration (default: the active Direct login's)")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the tenant the registration pins.</summary>
    [CommandOption("--tenant <TENANT>")]
    [Description("Tenant to pin in the registration (default: the active login's tenant when --url is not given, otherwise none)")]
    public string? Tenant { get; set; }

    /// <summary>Gets or sets whether the registration pins no tenant.</summary>
    [CommandOption("--no-tenant")]
    [Description("Pin no tenant, even when the active login has one")]
    public bool NoTenant { get; set; }
}

/// <summary>Registers the Direct stdio bridge in AI client configuration.</summary>
[LlmDescription("Register 'cratis direct mcp --url <origin> (--tenant <tenant> | --no-tenant)' as a stdio MCP server named 'cratis-direct' in AI client configuration: user scope (~/.claude.json, ~/.codex/config.toml, ~/.cursor/mcp.json, VS Code's user mcp.json, ~/.config/opencode/opencode.json) or project scope (.mcp.json, .codex/config.toml, .cursor/mcp.json, .vscode/mcp.json, opencode.json). Only the 'cratis-direct' member is written; an existing entry with that name that this command did not write is reported as a conflict and nothing is changed. Ownership is recorded in .cratis/direct-mcp.json in the home directory or project. Without --client, every client whose configuration exists is registered. The registration always pins both the origin and the tenant, writing '--no-tenant' when there is none, so a later 'cratis direct use' never redirects it. Without --url, --tenant or --no-tenant it pins the active 'cratis direct login', tenant included; with --url and no tenant option it pins no tenant. The pinned origin and tenant must have a stored login. Clients that cannot be registered in the scope (pi, or a configuration relocated by an environment variable) are reported, not guessed.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("install", "Register the Direct MCP bridge in AI clients", Branch = typeof(DirectBranch.Mcp))]
[CliExample("direct", "mcp", "install", "--client", "claude")]
[CliExample("direct", "mcp", "install", "--client", "codex", "--scope", "project", "--dry-run")]
[CliExample("direct", "mcp", "install", "--client", "cursor", "--tenant", "my-tenant")]
[LlmOption("--client", "string", "claude, codex, copilot, cursor, opencode or pi; comma-separated (default: every client whose configuration exists)")]
[LlmOption("--scope", "string", "user (default) or project")]
[LlmOption("--dry-run", "bool", "Show the exact member values without writing")]
[LlmOption("--url", "string", "Direct origin to pin (default: the active login's)")]
[LlmOption("--tenant", "string", "Tenant to pin (default: the active login's tenant when --url is not given, otherwise none)")]
[LlmOption("--no-tenant", "bool", "Pin no tenant, even when the active login has one")]
public sealed class DirectMcpInstallCommand : AsyncCommand<DirectMcpInstallSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DirectMcpInstallSettings settings, CancellationToken cancellationToken) =>
        Task.FromResult(DirectMcpRegistrationOutput.Run(settings, () =>
            DirectMcpRegistration.Install(settings.SelectedScope, DirectMcpLocations.Current, settings.SelectedClients, Pinned(settings))));

    /// <summary>Resolves the origin and tenant the bridge would use now, requiring a stored login for them.</summary>
    /// <param name="settings">The requested origin and tenant.</param>
    /// <returns>The launch arguments pinning them.</returns>
    /// <exception cref="AiMcpConfigurationInvalid">When --tenant and --no-tenant are combined.</exception>
    static IReadOnlyList<string> Pinned(DirectMcpInstallSettings settings)
    {
        if (settings.NoTenant && settings.Tenant is not null) throw new AiMcpConfigurationInvalid("--tenant and --no-tenant cannot be combined.");
        var (target, _) = DirectMcpRunner.Resolve(CliConfiguration.Load().Direct, new(settings.Url, settings.Tenant, settings.NoTenant));
        return DirectMcpClients.Arguments(target.Origin.ToString(), target.Tenant);
    }
}

/// <summary>Removes the Direct stdio bridge registrations this CLI wrote.</summary>
[LlmDescription("Remove the 'cratis-direct' MCP registrations that 'cratis direct mcp install' wrote, as recorded in .cratis/direct-mcp.json for the scope. An entry changed since it was installed is reported as a conflict and nothing is changed. Entries the command did not write are never touched.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("uninstall", "Remove the Direct MCP bridge from AI clients", Branch = typeof(DirectBranch.Mcp))]
[CliExample("direct", "mcp", "uninstall")]
[CliExample("direct", "mcp", "uninstall", "--client", "claude", "--dry-run")]
[LlmOption("--client", "string", "Only these clients (default: every registration this CLI wrote in the scope)")]
[LlmOption("--scope", "string", "user (default) or project")]
[LlmOption("--dry-run", "bool", "Show the exact member values that would be removed")]
public sealed class DirectMcpUninstallCommand : AsyncCommand<DirectMcpChangeSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DirectMcpChangeSettings settings, CancellationToken cancellationToken) =>
        Task.FromResult(DirectMcpRegistrationOutput.Run(settings, () =>
            DirectMcpRegistration.Uninstall(settings.SelectedScope, DirectMcpLocations.Current, settings.SelectedClients)));
}

/// <summary>Shows where the Direct stdio bridge is registered.</summary>
[LlmDescription("Show, per AI client, whether the 'cratis-direct' MCP registration is registered by this CLI, modified since, owned by the user, absent, or unsupported in the scope.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("status", "Show where the Direct MCP bridge is registered", Branch = typeof(DirectBranch.Mcp))]
[CliExample("direct", "mcp", "status", "--scope", "project")]
[LlmOption("--client", "string", "Only these clients (default: every client)")]
[LlmOption("--scope", "string", "user (default) or project")]
public sealed class DirectMcpStatusCommand : AsyncCommand<DirectMcpRegistrationSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, DirectMcpRegistrationSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var clients = DirectMcpRegistration.Status(settings.SelectedScope, DirectMcpLocations.Current, settings.SelectedClients);
            OutputFormatter.Write(format, clients, ["Client", "State", "Path", "Detail"], client => [client.Client, client.State, client.Path ?? string.Empty, client.Detail ?? string.Empty], client => $"{client.Client}\t{client.State}");
            return Task.FromResult(ExitCodes.Success);
        }
        catch (Exception ex) when (DirectMcpRegistrationOutput.IsSafeFailure(ex))
        {
            OutputFormatter.WriteError(format, "Direct MCP registration status could not be read", ex.Message, ExitCodes.ValidationErrorCode);
            return Task.FromResult(ExitCodes.ValidationError);
        }
    }
}

/// <summary>Applies a registration plan and reports it.</summary>
internal static class DirectMcpRegistrationOutput
{
    internal static bool IsSafeFailure(Exception ex) => ex is AiMcpConfigurationInvalid or IOException or UnauthorizedAccessException or JsonException or DirectAuthError;

    internal static int Run(DirectMcpChangeSettings settings, Func<DirectMcpRegistration> plan)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var registration = plan();
            var failed = registration.Conflicts.Count > 0;
            if (!failed) registration.Apply(new AiFileOperations(settings.DryRun));
            var result = new DirectMcpRegistrationResult(registration.Changes, registration.Conflicts, registration.Unsupported, settings.DryRun || failed);
            OutputFormatter.WriteObject(format, result, Render);
            if (registration.NothingRegistrable)
            {
                OutputFormatter.WriteError(format, "None of the selected MCP clients can be registered in this scope; nothing was changed.", "See the reasons above, or choose another client or --scope.", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }
            if (!failed) return ExitCodes.Success;
            OutputFormatter.WriteError(format, "An MCP entry named 'cratis-direct' is not owned by this CLI or changed since it was installed; nothing was changed.", "Remove or rename that entry yourself, then run the command again.", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
        catch (Exception ex) when (IsSafeFailure(ex))
        {
            OutputFormatter.WriteError(format, "Direct MCP registration failed", ex.Message, ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }

    static void Render(DirectMcpRegistrationResult result)
    {
        if (result.Changes.Count == 0 && result.Conflicts.Count == 0 && result.Unsupported.Count == 0) AnsiConsole.MarkupLine("No changes; the Direct MCP registrations are up to date.");
        foreach (var change in result.Changes)
        {
            var action = (change.Action, result.DryRun) switch
            {
                ("add", true) => "Would add",
                ("add", false) => "Added",
                ("update", true) => "Would update",
                ("update", false) => "Updated",
                (_, true) => "Would remove",
                _ => "Removed"
            };
            AnsiConsole.MarkupLine($"{action} [bold]{change.Member.EscapeMarkup()}[/] in {change.Path.EscapeMarkup()} ({change.Client.EscapeMarkup()}):");
            AnsiConsole.WriteLine(change.Value);
        }
        foreach (var conflict in result.Conflicts) AnsiConsole.MarkupLine($"[{OutputFormatter.Warning.ToMarkup()}]Conflict:[/] {conflict.EscapeMarkup()}");
        foreach (var unsupported in result.Unsupported) AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Unsupported:[/] {unsupported.EscapeMarkup()}");
    }
}

/// <summary>The outcome of a registration command.</summary>
/// <param name="Changes">The changes made, or that would be made.</param>
/// <param name="Conflicts">Entries that blocked the change.</param>
/// <param name="Unsupported">Clients that cannot be registered, with the reason.</param>
/// <param name="DryRun">Whether nothing was written.</param>
internal sealed record DirectMcpRegistrationResult(IReadOnlyList<DirectMcpChange> Changes, IReadOnlyList<string> Conflicts, IReadOnlyList<string> Unsupported, bool DryRun);
