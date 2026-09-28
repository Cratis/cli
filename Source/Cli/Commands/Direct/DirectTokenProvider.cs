// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Obtains a fresh access token for the active resource and tenant, for the future Direct MCP bridge.</summary>
internal interface IDirectTokenProvider
{
    Task<string> GetAccessToken(DirectTarget target, Uri issuer, CancellationToken cancellationToken);
}

/// <summary>Acquires an exclusive per-credential lock across processes, before reading rotating refresh tokens.</summary>
internal interface IDirectRefreshLock
{
    Task<IAsyncDisposable> Acquire(string key, CancellationToken cancellationToken);
}

/// <summary>Secret token set; this type must never be sent to the output formatter.</summary>
/// <param name="AccessToken">Access token.</param>
/// <param name="RefreshToken">Refresh token.</param>
/// <param name="ExpiresAt">Access token expiry.</param>
/// <param name="Scopes">Granted scopes.</param>
internal sealed record DirectTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, string Scopes);

/// <summary>File-backed advisory lock using exclusive sharing, with bounded cancellation and owner-only permissions.</summary>
/// <param name="home">User home directory.</param>
internal sealed class DirectRefreshLock(string home) : IDirectRefreshLock
{
    public async Task<IAsyncDisposable> Acquire(string key, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(home, ".cratis", "direct-locks");
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var path = Path.Combine(directory, key + ".lock");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        while (true)
        {
            try
            {
                var options = new FileStreamOptions { Mode = FileMode.OpenOrCreate, Access = FileAccess.ReadWrite, Share = FileShare.None };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }

                var stream = new FileStream(path, options);
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }

                return stream;
            }
            catch (IOException)
            {
                try
                {
                    await Task.Delay(75, deadline.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new DirectAuthError("Timed out waiting for another Direct token refresh.");
                }
            }
        }
    }
}

/// <summary>Owns token exchange, atomic refresh under a per-target lock, and revocation.</summary>
/// <param name="store">Secret store.</param>
/// <param name="refreshLock">Cross-process lock.</param>
/// <param name="discovery">Issuer discovery.</param>
/// <param name="http">HTTP transport.</param>
/// <param name="authorizationIssuer">Expected issuer.</param>
internal sealed class DirectTokenProvider(IDirectSecretStore store, IDirectRefreshLock refreshLock, DirectDiscovery discovery, HttpClient http, Uri authorizationIssuer) : IDirectTokenProvider
{
    public async Task<string> GetAccessToken(DirectTarget target, Uri issuer, CancellationToken cancellationToken)
    {
        if (authorizationIssuer != issuer)
        {
            throw new DirectAuthError("Stored Direct issuer does not match the selected issuer.");
        }

        var tokens = await Read(target, cancellationToken) ?? throw new DirectAuthError("Not logged in to Direct. Run 'cratis direct login'.");
        if (!string.IsNullOrEmpty(tokens.AccessToken) && tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return tokens.AccessToken;
        }

        // Re-read after the lock: another process may already have rotated the refresh token.
        await using var held = await refreshLock.Acquire(target.Key, cancellationToken);
        tokens = await Read(target, cancellationToken) ?? throw new DirectAuthError("Direct session was logged out. Run 'cratis direct login'.");
        if (!string.IsNullOrEmpty(tokens.AccessToken) && tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return tokens.AccessToken;
        }

        var endpoints = await discovery.DiscoverIssuer(authorizationIssuer, cancellationToken);
        var refreshed = await Exchange(
        endpoints.Token,
        target,
        new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["refresh_token"] = tokens.RefreshToken,
            ["client_id"] = "cratis-cli"
        },
        tokens.RefreshToken,
        cancellationToken,
        tokens.Scopes);
        await Save(target, refreshed, cancellationToken);
        return refreshed.AccessToken;
    }

    internal async Task<DirectTokens?> Read(DirectTarget target, CancellationToken cancellationToken)
    {
        var json = await store.Read(target.Key, cancellationToken);
        if (json is null)
        {
            return null;
        }

        try
        {
            var tokens = JsonSerializer.Deserialize<DirectTokens>(json);
            if (tokens is null || (string.IsNullOrWhiteSpace(tokens.AccessToken) && store is not WindowsDirectSecrets) || string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                throw new DirectAuthError("Stored Direct credentials are invalid.");
            }

            return tokens;
        }
        catch (JsonException)
        {
            throw new DirectAuthError("Stored Direct credentials are invalid.");
        }
    }

    internal Task Save(DirectTarget target, DirectTokens tokens, CancellationToken cancellationToken) =>
        store.Write(target.Key, JsonSerializer.Serialize(tokens), cancellationToken);

    internal async Task<DirectTokens> Exchange(Uri endpoint, DirectTarget target, IDictionary<string, string> parameters, string? previousRefresh, CancellationToken cancellationToken, string? previousScopes = null)
    {
        parameters["resource"] = target.Resource.AbsoluteUri;
        using var form = new FormUrlEncodedContent(parameters);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = form };
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Do not include the response body (it could contain credentials).
            throw new DirectAuthError($"Authorization server rejected token exchange (HTTP {(int)response.StatusCode}). Run 'cratis direct login' if the session expired.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var bytes = new byte[65537];
        var length = 0;
        while (length < bytes.Length)
        {
            var count = await stream.ReadAsync(bytes.AsMemory(length), cancellationToken);
            if (count == 0)
            {
                break;
            }

            length += count;
        }

        if (length > 65536)
        {
            throw new DirectAuthError("Token response is too large.");
        }

        try
        {
            using var document = JsonDocument.Parse(bytes.AsMemory(0, length));
            var root = document.RootElement;
            var access = root.GetProperty("access_token").GetString();
            var refresh = root.TryGetProperty("refresh_token", out var rotated) ? rotated.GetString() : previousRefresh;
            var seconds = root.GetProperty("expires_in").GetInt64();
            var type = root.GetProperty("token_type").GetString();
            var scopes = root.TryGetProperty("scope", out var scope) ? scope.GetString() : null;
            if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh) ||
                !string.Equals(type, "Bearer", StringComparison.OrdinalIgnoreCase) || seconds <= 60 || seconds > 86400 * 30)
            {
                throw new DirectAuthError("Authorization server returned an unusable token response.");
            }

            return new DirectTokens(access, refresh, DateTimeOffset.UtcNow.AddSeconds(seconds), scopes ?? previousScopes ?? string.Empty);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            throw new DirectAuthError("Authorization server returned an unusable token response.");
        }
    }

    internal async Task<bool> Revoke(DirectTarget target, CancellationToken cancellationToken)
    {
        await using var held = await refreshLock.Acquire(target.Key, cancellationToken);
        var tokens = await Read(target, cancellationToken);
        if (tokens is null)
        {
            return false;
        }

        var endpoints = await discovery.DiscoverIssuer(authorizationIssuer, cancellationToken);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = tokens.RefreshToken, ["token_type_hint"] = "refresh_token", ["client_id"] = "cratis-cli"
        });
        using var response = await http.PostAsync(endpoints.Revocation, form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new DirectAuthError($"Authorization server did not revoke the refresh token (HTTP {(int)response.StatusCode}); local credentials were retained.");
        }

        await store.Delete(target.Key, cancellationToken);
        return true;
    }
}
