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
/// <param name="Issuer">
/// The authorization server that issued the tokens. It is kept with the secret, so a tampered CLI configuration
/// cannot redirect the refresh token to another server. Null for credentials saved before it was recorded.
/// </param>
internal sealed record DirectTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, string Scopes, string? Issuer = null);

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

        EnsureIssuedBy(tokens);
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
        store.Write(target.Key, JsonSerializer.Serialize(tokens with { Issuer = authorizationIssuer.OriginalString }), cancellationToken);

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

            return new DirectTokens(access, refresh, DateTimeOffset.UtcNow.AddSeconds(seconds), scopes ?? previousScopes ?? string.Empty, authorizationIssuer.OriginalString);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            throw new DirectAuthError("Authorization server returned an unusable token response.");
        }
    }

    /// <summary>Revokes the stored refresh token, then deletes the local credential. A failed revocation keeps it.</summary>
    /// <param name="target">The credential target.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>True when a stored credential was revoked and deleted; false when none was stored.</returns>
    /// <exception cref="DirectAuthError">When the authorization server does not confirm the revocation.</exception>
    internal async Task<bool> Revoke(DirectTarget target, CancellationToken cancellationToken)
    {
        await using var held = await refreshLock.Acquire(target.Key, cancellationToken);
        var tokens = await Read(target, cancellationToken);
        if (tokens is null)
        {
            return false;
        }

        EnsureIssuedBy(tokens);
        var status = await PostRevocation(tokens.RefreshToken, cancellationToken);
        if (status is not null)
        {
            throw new DirectAuthError($"Authorization server did not revoke the refresh token (HTTP {status}); local credentials were retained.");
        }

        await store.Delete(target.Key, cancellationToken);
        return true;
    }

    /// <summary>
    /// Saves newly issued tokens under the refresh lock, after superseding the credential they replace. If the new tokens
    /// cannot be saved, their refresh token is revoked (best effort) before the failure is rethrown, so it is not orphaned.
    /// </summary>
    /// <param name="target">The credential target.</param>
    /// <param name="tokens">The newly issued tokens, obtained from this provider's issuer.</param>
    /// <param name="previous">The provider for the credential being replaced, with its own store and issuer.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A warning without credentials about the replaced credential; otherwise null.</returns>
    internal async Task<string?> Replace(DirectTarget target, DirectTokens tokens, DirectTokenProvider previous, CancellationToken cancellationToken)
    {
        var saved = false;
        try
        {
            await using var held = await refreshLock.Acquire(target.Key, cancellationToken);
            var warning = await previous.Supersede(target, cancellationToken);
            await Save(target, tokens, cancellationToken);
            saved = true;
            return warning;
        }
        catch (Exception ex) when (!saved && DirectLoginFlow.IsSafeFailure(ex))
        {
            await RevokeQuietly(tokens.RefreshToken);
            throw;
        }
    }

    /// <summary>
    /// Revokes, best effort, a stored credential that is about to be replaced, and removes it locally either way.
    /// Store and revocation failures become a warning, because the new login must still complete.
    /// The caller must hold the refresh lock for the target.
    /// </summary>
    /// <param name="target">The credential target.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A warning without credentials when the previous credential was not cleanly revoked and removed; otherwise null.</returns>
    internal async Task<string?> Supersede(DirectTarget target, CancellationToken cancellationToken)
    {
        DirectTokens? tokens;
        try
        {
            tokens = await Read(target, cancellationToken);
        }
        catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
        {
            return await Remove(target, "The previous Direct credential was unreadable and could not be revoked.", cancellationToken);
        }

        if (tokens is null)
        {
            return null;
        }

        string? failure;
        try
        {
            EnsureIssuedBy(tokens);
            var status = await PostRevocation(tokens.RefreshToken, cancellationToken);
            failure = status is null ? null : $"The authorization server did not revoke the previous Direct refresh token (HTTP {status}).";
        }
        catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
        {
            failure = "The previous Direct refresh token could not be revoked: " + (ex is DirectAuthError ? ex.Message : "the authorization server was unreachable.");
        }

        return await Remove(target, failure, cancellationToken);
    }

    static bool IsRecoverable(Exception ex, CancellationToken cancellationToken) =>
        DirectLoginFlow.IsSafeFailure(ex) && !cancellationToken.IsCancellationRequested;

    async Task<string?> Remove(DirectTarget target, string? failure, CancellationToken cancellationToken)
    {
        try
        {
            await store.Delete(target.Key, cancellationToken);
        }
        catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
        {
            return (failure ?? "The previous Direct refresh token was revoked.") + " The replaced credential could not be removed from its credential store" +
                (ex is DirectAuthError ? $": {ex.Message}" : ".");
        }

        return failure is null
            ? null
            : failure + " The replaced credential was removed from this machine; if still valid, it expires on its own or can be revoked at the authorization server.";
    }

    async Task RevokeQuietly(string refreshToken)
    {
        try
        {
            // Not canceled with the login: an unsaved refresh token must not be left valid.
            await PostRevocation(refreshToken, CancellationToken.None);
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            // Best effort; the original failure is what the user needs to see.
        }
    }

    /// <summary>Refuses to send a token unless the configured issuer is the one recorded with the secret at login.</summary>
    /// <param name="tokens">The stored tokens.</param>
    /// <exception cref="DirectAuthError">When the issuers differ or the credential records none.</exception>
    void EnsureIssuedBy(DirectTokens tokens)
    {
        if (tokens.Issuer is null)
        {
            throw new DirectAuthError("The stored Direct credential does not record which authorization server issued it, so no token was sent. Run 'cratis direct login' again for this origin and tenant.");
        }

        if (!string.Equals(tokens.Issuer, authorizationIssuer.OriginalString, StringComparison.Ordinal))
        {
            throw new DirectAuthError("The configured Direct issuer differs from the one that issued the stored credential, so no token was sent. Check ~/.cratis/config.json, or run 'cratis direct login' again for this origin and tenant.");
        }
    }

    async Task<int?> PostRevocation(string refreshToken, CancellationToken cancellationToken)
    {
        var endpoints = await discovery.DiscoverIssuer(authorizationIssuer, cancellationToken);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken, ["token_type_hint"] = "refresh_token", ["client_id"] = "cratis-cli"
        });
        using var response = await http.PostAsync(endpoints.Revocation, form, cancellationToken);
        return response.IsSuccessStatusCode ? null : (int)response.StatusCode;
    }
}
