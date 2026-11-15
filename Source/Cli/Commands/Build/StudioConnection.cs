// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Studio;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The Studio this machine is connected to, with its credentials read from the credential store: the builder client and,
/// when the organization's MCP credential is configured, the client for Studio's MCP tools.
/// </summary>
/// <param name="Configuration">The connection's configuration.</param>
/// <param name="Builder">The builder client, authenticated when the builder is registered.</param>
/// <param name="Mcp">The MCP client, or <see langword="null"/> without an MCP credential.</param>
/// <param name="Secrets">Every credential the connection holds, for redaction.</param>
/// <param name="IsRegistered">Whether the builder is registered and its token is stored.</param>
internal sealed record StudioConnection(StudioConfiguration Configuration, StudioBuilderClient Builder, StudioMcpClient? Mcp, IReadOnlyList<string> Secrets, bool IsRegistered)
{
    /// <summary>
    /// The HTTP client every Studio call uses; a held request for work stays open up to a minute.
    /// </summary>
    public static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    /// <summary>
    /// Opens the configured connection.
    /// </summary>
    /// <param name="cancellationToken">Cancels reading the credentials.</param>
    /// <returns>The connection.</returns>
    /// <exception cref="StudioNotConnected">This machine is not connected to a Studio.</exception>
    public static async Task<StudioConnection> Open(CancellationToken cancellationToken)
    {
        var configuration = CliConfiguration.Load().Studio;
        if (configuration is null || string.IsNullOrWhiteSpace(configuration.Url) || string.IsNullOrWhiteSpace(configuration.Organization))
        {
            throw new StudioNotConnected("This machine is not connected to Cratis Studio. Run 'cratis build connect --studio <url> --organization <name>' first.");
        }

        var credentials = StudioCredentials.For(configuration);
        var builder = new StudioBuilderClient(Http, new Uri(configuration.Url), configuration.Organization);
        var token = await credentials.BuilderToken(configuration, cancellationToken);
        if (configuration.BuilderId is { } builderId && token is not null) builder.Authenticate(builderId, token);

        var secret = await credentials.McpSecret(configuration, cancellationToken);
        var mcp = configuration is { McpUrl: { } url, McpClientId: { } clientId } && secret is not null
            ? new StudioMcpClient(Http, new Uri(url), clientId, secret)
            : null;
        return new(configuration, builder, mcp, [.. new[] { token, secret }.OfType<string>()], configuration.BuilderId is not null && token is not null);
    }
}
