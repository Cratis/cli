// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Lightweight CLI wrapper around <see cref="ChronicleConnection"/> for use in CLI commands.
/// </summary>
/// <param name="connection">The underlying <see cref="ChronicleConnection"/>.</param>
/// <param name="cts">The linked <see cref="CancellationTokenSource"/> used for lifecycle control.</param>
public sealed class CliChronicleConnection(ChronicleConnection connection, CancellationTokenSource cts) : IDisposable
{
    /// <summary>
    /// Gets the gRPC service proxies.
    /// </summary>
    public IServices Services => ((IChronicleServicesAccessor)connection).Services;

    /// <summary>
    /// Creates and connects a <see cref="CliChronicleConnection"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">The parsed Chronicle connection string.</param>
    /// <param name="managementPort">The management port for the token endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A connected <see cref="CliChronicleConnection"/>.</returns>
    public static async Task<CliChronicleConnection> Connect(ChronicleConnectionString connectionString, int managementPort, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2000 // cts and connection ownership is transferred to CliChronicleConnection on success; both are disposed in the catch block on failure
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ChronicleConnection? connection = null;
#pragma warning restore CA2000

        try
        {
#pragma warning disable CA2000 // tokenProvider ownership is transferred to ChronicleConnection which disposes it
            var config = CliConfiguration.Load();
            var tokenProvider = connectionString.AuthenticationMode switch
            {
                AuthenticationMode.ClientCredentials => (ITokenProvider?)CreateCachingTokenProvider(connectionString, managementPort, config.ActiveContextName),
                AuthenticationMode.ApiKey when !string.IsNullOrEmpty(connectionString.ApiKey) =>
                    new StaticTokenProvider(connectionString.ApiKey),
                _ => null
            };
#pragma warning restore CA2000

            var connectTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("CHRONICLE_CONNECT_TIMEOUT_SECONDS"), out var t) ? t : 5;
#pragma warning disable CA2000 // connection ownership is transferred to CliChronicleConnection on success; disposed in catch on failure
            connection = new ChronicleConnection(
#pragma warning restore CA2000
                connectionString,
                connectTimeout: connectTimeoutSeconds,
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

            // Eagerly fetch the token so HttpRequestException from an unreachable management port
            // propagates directly instead of being wrapped as RpcException by the gRPC interceptor.
            if (tokenProvider is not null)
            {
                await tokenProvider.GetAccessToken(cts.Token);
            }

            await connection.Connect();

            return new CliChronicleConnection(connection, cts);
        }
        catch
        {
            connection?.Dispose();
            cts.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates and connects a <see cref="CliChronicleConnection"/> synchronously (for use in synchronous interceptors).
    /// Spectre.Console's <see cref="Spectre.Console.Cli.ICommandInterceptor"/> interface is synchronous;
    /// blocking on async here is intentional and safe because this is always called from a thread-pool context without a synchronization context.
    /// </summary>
    /// <param name="connectionString">The parsed Chronicle connection string.</param>
    /// <param name="managementPort">The management port for the token endpoint.</param>
    /// <returns>A connected <see cref="CliChronicleConnection"/>.</returns>
#pragma warning disable CA2000 // Caller is responsible for disposing the returned instance
    public static CliChronicleConnection ConnectSync(ChronicleConnectionString connectionString, int managementPort)
        => Connect(connectionString, managementPort).GetAwaiter().GetResult();
#pragma warning restore CA2000

    /// <summary>
    /// Deletes the cached token for a given context and username combination, if it exists.
    /// Call this when a cached token is rejected by the server (e.g. HTTP 401) so the next
    /// connection attempt will fetch a fresh token.
    /// </summary>
    /// <param name="contextName">The active context name.</param>
    /// <param name="username">The client ID / username associated with the cached token.</param>
    public static void ClearTokenCache(string contextName, string username)
    {
        var cachePath = CliConfiguration.GetTokenCachePath($"{contextName}_{username}");
        if (File.Exists(cachePath))
        {
            File.Delete(cachePath);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
        connection.Dispose();
    }

    static FileSystemCachingTokenProvider CreateCachingTokenProvider(ChronicleConnectionString connectionString, int managementPort, string contextName)
    {
        var cachePath = CliConfiguration.GetTokenCachePath($"{contextName}_{connectionString.Username ?? string.Empty}");
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
#pragma warning disable CA2000 // inner ownership is transferred to FileSystemCachingTokenProvider which disposes it
        var inner = new OAuthTokenProvider(
            connectionString.ServerAddress,
            connectionString.Username ?? string.Empty,
            connectionString.Password ?? string.Empty,
            managementPort,
            connectionString.DisableTls,
            NullLogger<OAuthTokenProvider>.Instance);
#pragma warning restore CA2000
        return new FileSystemCachingTokenProvider(inner, cachePath);
    }
}
