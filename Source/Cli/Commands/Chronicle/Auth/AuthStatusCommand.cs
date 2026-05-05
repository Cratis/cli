// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Shows the current authentication status including user or client credentials.
/// </summary>
[LlmDescription("Shows the current authentication status including token expiry and the authenticated identity. Use to verify login state before running authenticated commands.")]
[CliCommand("status", "Show current authentication status", Branch = typeof(ChronicleBranch.Auth))]
[CliExample("chronicle", "auth", "status")]
[LlmOutputAdvice("json", "JSON is structured for key-value parsing. Use JSON when checking auth state programmatically.")]
public class AuthStatusCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var contextName = config.ActiveContextName;
        var ctx = config.GetCurrentContext();

        var status = new AuthStatusInfo
        {
            Context = contextName,
            LoggedInUser = ctx.LoggedInUser,
            ClientId = ctx.ClientId,
            HasClientSecret = !string.IsNullOrWhiteSpace(ctx.ClientSecret),
            Server = ctx.Server
        };

        OutputFormatter.WriteObject(format, status, s =>
        {
            AnsiConsole.MarkupLine($"[bold]Context:[/]         {s.Context.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Server:[/]          {(s.Server ?? "(not set)").EscapeMarkup()}");
            AnsiConsole.WriteLine();

            if (!string.IsNullOrWhiteSpace(s.LoggedInUser))
            {
                AnsiConsole.MarkupLine("[bold]User Session:[/]");
                AnsiConsole.MarkupLine($"  Username: {s.LoggedInUser.EscapeMarkup()}");
            }
            else if (!string.IsNullOrWhiteSpace(s.ClientId))
            {
                AnsiConsole.MarkupLine("[bold]Client Credentials:[/]");
                AnsiConsole.MarkupLine($"  Client ID:     {s.ClientId.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"  Client Secret: {(s.HasClientSecret ? "[dim]********[/]" : "[dim](not set)[/]")}");
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]Not logged in — run 'cratis chronicle login <USERNAME>'[/]");
            }
        });

        return Task.FromResult(ExitCodes.Success);
    }

    sealed record AuthStatusInfo
    {
        public string Context { get; init; } = string.Empty;
        public string? LoggedInUser { get; init; }
        public string? ClientId { get; init; }
        public bool HasClientSecret { get; init; }
        public string? Server { get; init; }
    }
}
