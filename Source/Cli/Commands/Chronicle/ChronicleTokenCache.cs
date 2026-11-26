// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Binds client-credential cache entries to one identity and both selected destinations.
/// </summary>
internal static class ChronicleTokenCache
{
    const string CacheVersion = "v2";

    internal static string? GetCachePath(ChronicleConnectionString connectionString, string contextName) =>
        GetDestination(connectionString) is { } destination
            ? GetCachePath(destination, new(contextName, connectionString.Username ?? string.Empty))
            : null;

    internal static ITokenProvider Create(ChronicleConnectionString connectionString, string contextName)
    {
        var destination = GetDestination(connectionString);
        var identity = new Identity(contextName, connectionString.Username ?? string.Empty);
        var path = destination is null ? null : GetCachePath(destination, identity);
        var scope = destination is null ? null : GetScope(destination, identity);

        // The returned provider owns the OAuth provider, including on the memory-only path.
#pragma warning disable CA2000 // Ownership is transferred to the caller or caching provider.
        var inner = new OAuthTokenProvider(
            destination?.Server ?? connectionString.ServerAddress,
            identity.ClientId,
            connectionString.Password ?? string.Empty,
            connectionString.SkipTlsValidation,
            NullLogger<OAuthTokenProvider>.Instance);
#pragma warning restore CA2000

        return path is null
            ? inner
            : new FileSystemCachingTokenProvider(inner, path, cacheScope: scope);
    }

    internal static void Clear(ChronicleConnectionString connectionString, string contextName) =>
        Delete(GetCachePath(connectionString, contextName));

    internal static void ClearIdentity(string contextName, string clientId)
    {
        var directory = Path.GetDirectoryName(CliConfiguration.GetTokenCachePath(string.Empty))!;
        if (Directory.Exists(directory))
        {
            var pattern = $"{GetIdentityPrefix(new(contextName, clientId))}*.token";
            foreach (var path in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
            {
                Delete(path);
            }
        }

        // Old entries are never read or migrated, but local logout still removes them.
        Delete(CliConfiguration.GetTokenCachePath($"{contextName}_{clientId}"));
    }

    static Destination? GetDestination(ChronicleConnectionString connectionString)
    {
        if (connectionString.IsSrv || connectionString.ServerAddresses.Count != 1)
        {
            return null;
        }

        var authority = TokenServer.Normalize(connectionString.ServerAddress);
        if (!Uri.TryCreate($"https://{authority}/connect/token", UriKind.Absolute, out var endpoint) || !IsTokenEndpoint(endpoint))
        {
            return null;
        }

        var host = endpoint.HostNameType == UriHostNameType.IPv6 ? $"[{endpoint.DnsSafeHost}]" : endpoint.Host;
        var server = new ChronicleServerAddress(host, endpoint.Port);
        authority = TokenServer.Normalize(server);
        var scheme = connectionString.DisableTls ? "http" : "https";
        return new(server, $"https://{authority}/connect/token", $"{scheme}://{authority}");
    }

    static bool IsTokenEndpoint(Uri endpoint) => endpoint is
    {
        HostNameType: not UriHostNameType.Unknown,
        Port: > 0,
        UserInfo.Length: 0,
        AbsolutePath: "/connect/token",
        Query.Length: 0,
        Fragment.Length: 0
    };

    static string GetCachePath(Destination destination, Identity identity)
    {
        var destinations = new[] { destination.TokenEndpoint, destination.ChronicleOrigin };
        return CliConfiguration.GetTokenCachePath($"{GetIdentityPrefix(identity)}{Hash(destinations)}");
    }

    static string GetScope(Destination destination, Identity identity) =>
        JsonSerializer.Serialize(new[] { CacheVersion, identity.ContextName, identity.ClientId, destination.TokenEndpoint, destination.ChronicleOrigin });

    static string GetIdentityPrefix(Identity identity) =>
        $"{CacheVersion}_{Hash(new[] { identity.ContextName, identity.ClientId })}_";

    static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));

    static void Delete(string? path)
    {
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    sealed record Identity(string ContextName, string ClientId);

    sealed record Destination(ChronicleServerAddress Server, string TokenEndpoint, string ChronicleOrigin);
}
