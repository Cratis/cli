// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Registers this machine with a Studio organization as a builder and waits for a maintainer to approve it.
/// </summary>
[CommandEffect(CommandEffect.Mutating)]
[CliCommand(
    "connect",
    "Register this machine as a builder with a Studio organization. Shows a short code a maintainer types under Settings > Builders to approve it; the builder's token is created here and kept in the operating system's credential store.",
    Branch = typeof(BuildBranch))]
[CliExample("build", "connect", "--studio", "--organization", "acme")]
[CliExample("build", "connect", "--studio", "https://studio.example.com", "--organization", "acme")]
[CliExample("build", "connect", "--studio", "--organization", "acme", "--mcp-client-id", "acme")]
public sealed class BuildConnectCommand : AsyncCommand<BuildConnectSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, BuildConnectSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var configuration = CliConfiguration.Load();
            var requested = StudioAddress.From(settings.Studio, settings.Organization);
            var studio = Target(configuration.Studio, requested);
            if (studio.Organization.Length == 0)
            {
                OutputFormatter.WriteError(format, "Name the organization with --organization <name>. Add --studio <url> only for a Studio other than the hosted one.", errorCode: ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }
            studio.InsecureFileStore = settings.InsecureFileStore || studio.InsecureFileStore;
            var client = BuilderTransport.Connect(studio.Url, studio.Organization);
            var credentials = StudioCredentials.For(studio);
            var token = settings.Force ? null : await credentials.BuilderToken(studio, cancellationToken);
            if (studio.BuilderId is null || token is null)
            {
                token = StudioCredentials.NewToken();
                var name = settings.Name ?? studio.BuilderName ?? Environment.MachineName;
                var registration = await client.Register(name, Environment.MachineName, token, cancellationToken);
                studio.BuilderId = registration.BuilderId;
                studio.BuilderName = name;
                await credentials.SaveBuilderToken(studio, token, cancellationToken);
                AnsiConsole.MarkupLineInterpolated($"Registered [bold]{name}[/] with {studio.Organization}. Ask a maintainer to approve it in Studio under Settings > Builders with the code [bold]{registration.Code}[/] before {registration.Expires.ToLocalTime():t}.");
            }

            await ConfigureMcp(settings, studio, credentials, cancellationToken);
            configuration.Studio = studio;
            configuration.Save();
            client.Authenticate(studio.BuilderId, token);
            if (settings.NoWait) return ExitCodes.Success;

            var loop = new BuildLoop(client, (order, _, _) => Task.FromResult(new JsonObject()), BuildHostFactory.Log);
            var welcome = await loop.Connect(BuilderManifests.For(null, "cratis build"), cancellationToken);
            await client.Disconnect(cancellationToken);
            OutputFormatter.WriteMessage(format, $"{welcome.Name} is approved and connected to {studio.Organization}. Start building with 'cratis build run'.");
            return ExitCodes.Success;
        }
        catch (StudioNotConnected invalid)
        {
            OutputFormatter.WriteError(format, invalid.Message, errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
        catch (StudioBuilderRefused refused)
        {
            OutputFormatter.WriteError(format, refused.Message, refused.CredentialRejected ? "Register again with 'cratis build connect --force'." : null, ExitCodes.AuthenticationErrorCode);
            return refused.CredentialRejected ? ExitCodes.AuthenticationError : ExitCodes.ServerError;
        }
        catch (Exception exception) when (exception is HttpRequestException or DirectAuthError)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }
    }

    static StudioConfiguration Target(StudioConfiguration? current, StudioAddress requested)
    {
        if (current is not null && Matches(current, requested)) return current;
        return new() { Url = requested.Url ?? StudioAddress.Hosted, Organization = requested.Organization ?? string.Empty };
    }

    static bool Matches(StudioConfiguration current, StudioAddress requested) =>
        (requested.Url is null || Same(current.Url, requested.Url)) && (requested.Organization is null || current.Organization == requested.Organization);

    static bool Same(string left, string right) => string.Equals(left.TrimEnd('/'), right.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

    static async Task ConfigureMcp(BuildConnectSettings settings, StudioConfiguration studio, StudioCredentials credentials, CancellationToken cancellationToken)
    {
        if (settings.McpClientId is null) return;
        var secret = Environment.GetEnvironmentVariable(BuildConnectSettings.McpSecretVariable);
        if (string.IsNullOrEmpty(secret))
        {
            if (!GlobalSettings.IsInteractiveEnvironment()) throw new DirectAuthError($"Give the MCP client secret in {BuildConnectSettings.McpSecretVariable}.");
            secret = await new TextPrompt<string>("MCP client secret:").Secret().ShowAsync(AnsiConsole.Console, cancellationToken);
        }

        studio.McpClientId = settings.McpClientId;
        studio.McpUrl = settings.McpUrl?.TrimEnd('/') ?? await StudioMcpAddress(studio, cancellationToken);
        await credentials.SaveMcpSecret(studio, secret, cancellationToken);
    }

    static async Task<string> StudioMcpAddress(StudioConfiguration studio, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await StudioConnection.Http.GetAsync(new Uri(new Uri(studio.Url), "/api/builders/session"), cancellationToken);
            var tools = response.IsSuccessStatusCode ? JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken))?["tools"] as JsonArray : null;
            var url = tools?.OfType<JsonObject>().FirstOrDefault(tool => tool["name"]?.GetValue<string>() == BuildSessionTools.Studio)?["url"]?.GetValue<string>();
            if (url is not null) return url.EndsWith("/mcp", StringComparison.Ordinal) ? url[..^4] : url;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
        }

        return "https://mcp.cratis.studio";
    }
}
