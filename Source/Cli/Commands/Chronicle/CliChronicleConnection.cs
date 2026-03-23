// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Lightweight CLI wrapper around <see cref="ChronicleConnection"/> for use in CLI commands.
/// </summary>
public sealed class CliChronicleConnection : IDisposable
{
    readonly ChronicleConnection _connection;
    readonly CancellationTokenSource _cts;

    CliChronicleConnection(ChronicleConnection connection, CancellationTokenSource cts)
    {
        _connection = connection;
        _cts = cts;
    }

    /// <summary>
    /// Gets the gRPC service proxies.
    /// </summary>
    public IServices Services => ((IChronicleServicesAccessor)_connection).Services;

    /// <summary>
    /// Creates and connects a <see cref="CliChronicleConnection"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">The parsed Chronicle connection string.</param>
    /// <param name="managementPort">The management port for the token endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A connected <see cref="CliChronicleConnection"/>.</returns>
    public static async Task<CliChronicleConnection> Connect(ChronicleConnectionString connectionString, int managementPort, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

#pragma warning disable CA2000 // tokenProvider ownership is transferred to ChronicleConnection which disposes it
        ITokenProvider? tokenProvider = connectionString.AuthenticationMode switch
        {
            AuthenticationMode.ClientCredentials => new OAuthTokenProvider(
                connectionString.ServerAddress,
                connectionString.Username ?? string.Empty,
                connectionString.Password ?? string.Empty,
                managementPort,
                connectionString.DisableTls,
                NullLogger<OAuthTokenProvider>.Instance),
            AuthenticationMode.ApiKey when !string.IsNullOrEmpty(connectionString.ApiKey) =>
                new StaticTokenProvider(connectionString.ApiKey),
            _ => null
        };
#pragma warning restore CA2000

        var connection = new ChronicleConnection(
            connectionString,
            connectTimeout: 5,
            maxReceiveMessageSize: null,
            maxSendMessageSize: null,
            new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance),
            new Tasks.TaskFactory(),
            new CorrelationIdAccessor(),
            NullLoggerFactory.Instance,
            cts.Token,
            NullLogger<ChronicleConnection>.Instance,
            connectionString.DisableTls,
            connectionString.CertificatePath,
            connectionString.CertificatePassword,
            tokenProvider,
            skipCompatibilityCheck: true,
            skipKeepAlive: true);

        await connection.Connect();

        return new CliChronicleConnection(connection, cts);
    }

    /// <summary>
    /// Creates and connects a <see cref="CliChronicleConnection"/> synchronously (for use in synchronous interceptors).
    /// </summary>
    /// <param name="connectionString">The parsed Chronicle connection string.</param>
    /// <param name="managementPort">The management port for the token endpoint.</param>
    /// <returns>A connected <see cref="CliChronicleConnection"/>.</returns>
#pragma warning disable CA2000 // Caller is responsible for disposing the returned instance
    public static CliChronicleConnection ConnectSync(ChronicleConnectionString connectionString, int managementPort)
        => Connect(connectionString, managementPort).GetAwaiter().GetResult();
#pragma warning restore CA2000

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _connection.Dispose();
    }
}
