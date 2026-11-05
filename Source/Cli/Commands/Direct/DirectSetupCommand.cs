// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Settings for signing in and registering Direct with local MCP clients.</summary>
public sealed class DirectSetupSettings : DirectTokenSettings
{
    /// <summary>Gets or sets the clients to configure; omitted means detected clients.</summary>
    [CommandOption("--client <CLIENT>")]
    [Description("Client names, repeated or comma-separated; omitted detects existing client configuration directories")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for repeatable options.")]
    public string[] Clients { get; set; } = [];

    /// <summary>Gets or sets the tenant hint sent to the authorization server.</summary>
    [CommandOption("--tenant <TENANT>")]
    [Description("Tenant hint passed to the authorization server (not proof of membership)")]
    public string? Tenant { get; set; }

    /// <summary>Gets or sets the configuration scope.</summary>
    [CommandOption("--scope <SCOPE>")]
    [Description("Configuration scope: user (default) or project")]
    public string Scope { get; set; } = "user";

    /// <summary>Gets or sets whether to preview without login, token refresh or file writes.</summary>
    [CommandOption("--dry-run")]
    [Description("Preview login and registrations without opening a browser, refreshing tokens or writing files")]
    public bool DryRun { get; set; }
}

/// <summary>Signs in once, then registers Direct with selected local MCP clients.</summary>
[CliCommand("setup", "Sign in to Direct and configure local MCP clients", Branch = typeof(DirectBranch))]
[Description("Sign in to Direct and configure local MCP clients")]
[CommandEffect(CommandEffect.Mutating)]
[CliExample("direct", "setup")]
[CliExample("direct", "setup", "--client", "claude,codex", "--dry-run")]
[LlmOption("--url", "string", "Direct HTTPS origin (default https://app.cratis.direct)")]
[LlmOption("--client", "string[]", "Repeated or comma-separated client names; omitted detects local configuration directories")]
[LlmOption("--scope", "string", "user (default) or project")]
[LlmOption("--dry-run", "bool", "Preview without login, token refresh or file writes")]
[LlmDescription("Sign in to Direct once and register cratis direct mcp for detected or explicitly selected clients. Dry-run does not log in, refresh tokens or write files.")]
public sealed class DirectSetupCommand : AsyncCommand<DirectSetupSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DirectSetupSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var config = CliConfiguration.Load().Direct;
            var result = await DirectSetup.Run(
                settings,
                DirectMcpLocations.Current,
                config,
                (target, ct) => HasLogin(config, target, ct),
                (login, ct) => login.Token.IsSet
                    ? DirectPersonalAccessLogin.Execute(login, login.Tenant, ct, report: false)
                    : DirectLoginFlow.Execute(login, login.Tenant, ct, report: false),
                cancellationToken);
            OutputFormatter.WriteObject(settings.ResolveOutputFormat(), result, value => Render(AnsiConsole.Console, value));
            return result.ExitCode;
        }
        catch (Exception ex) when (ex is AiMcpConfigurationInvalid || DirectLoginFlow.IsSafeFailure(ex))
        {
            return DirectLoginFlow.Fail(settings, ex);
        }
    }

    static async Task<bool> HasLogin(DirectConfiguration? config, DirectTarget target, CancellationToken cancellationToken)
    {
        try
        {
            var (_, credential) = DirectMcpRunner.Resolve(config, new(DirectCredentials.OriginOf(target), target.Tenant, NoTenant: target.Tenant is null));
            using var http = DirectLoginFlow.CreateHttp();
            await DirectLoginFlow.TokenProviderFor(credential, http).GetAccessToken(target, credential.Issuer is null ? target.Origin : new Uri(credential.Issuer), cancellationToken);
            return true;
        }
        catch (DirectAuthError)
        {
            return false;
        }
    }

    static void Render(IAnsiConsole console, DirectSetupResult result)
    {
        console.MarkupLine(Markup.Escape(result.Login));
        foreach (var client in result.Clients)
        {
            console.MarkupLine(Markup.Escape($"{client.Client}: {client.State}{(client.Reason is null ? string.Empty : $" — {client.Reason}")}"));
        }
    }
}
