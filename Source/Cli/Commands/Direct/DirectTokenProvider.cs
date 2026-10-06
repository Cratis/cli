// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Obtains fresh access tokens for a Direct resource and tenant.</summary>
internal interface IDirectTokenProvider
{
    /// <summary>Gets a stored access token that is not about to expire, refreshing it when it is.</summary>
    /// <param name="target">The credential target.</param>
    /// <param name="issuer">The issuer the credential must have been granted by.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The access token.</returns>
    Task<string> GetAccessToken(DirectTarget target, Uri issuer, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces an access token the resource server rejected. Another process may already have refreshed it, in which
    /// case the newer stored token is returned without another refresh.
    /// </summary>
    /// <param name="target">The credential target.</param>
    /// <param name="issuer">The issuer the credential must have been granted by.</param>
    /// <param name="rejected">The access token the resource server rejected.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A different access token.</returns>
    Task<string> RefreshAccessToken(DirectTarget target, Uri issuer, string rejected, CancellationToken cancellationToken);
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
        if (IsUsable(tokens, null))
        {
            return tokens.AccessToken;
        }

        return await Refresh(target, null, cancellationToken);
    }

    public Task<string> RefreshAccessToken(DirectTarget target, Uri issuer, string rejected, CancellationToken cancellationToken)
    {
        if (authorizationIssuer != issuer)
        {
            throw new DirectAuthError("Stored Direct issuer does not match the selected issuer.");
        }

        return Refresh(target, rejected, cancellationToken);
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
                throw new DirectAuthError("Stored Direct credentials are invalid.", invalidCredential: true);
            }

            return tokens;
        }
        catch (JsonException)
        {
            throw new DirectAuthError("Stored Direct credentials are invalid.", invalidCredential: true);
        }
    }

    internal Task Save(DirectTarget target, DirectTokens tokens, CancellationToken cancellationToken) =>
        store.Write(target.Key, JsonSerializer.Serialize(tokens with { Issuer = authorizationIssuer.OriginalString }), cancellationToken);

    internal async Task<DirectTokens> Exchange(Uri endpoint, DirectTarget target, IDictionary<string, string> parameters, string? previousRefresh, CancellationToken cancellationToken, string? previousScopes = null)
    {
        using var deadline = DirectHttp.Deadline(http, cancellationToken);
        cancellationToken = deadline.Token;
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
    /// Saves and publishes newly issued tokens before revoking the previous credential, under the refresh lock.
    /// Failed publication revokes the new token and restores the previous secret or removes the new entry.
    /// </summary>
    /// <param name="target">The credential target.</param>
    /// <param name="tokens">The newly issued tokens, obtained from this provider's issuer.</param>
    /// <param name="previous">The provider for the credential being replaced, with its own store and issuer.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="publish">Publishes the non-secret index after the secret is saved; the caller holds the configuration lock.</param>
    /// <returns>A warning without credentials about the replaced credential; otherwise null.</returns>
    /// <exception cref="DirectAuthError">When recovery cannot revoke or clean up the new credential.</exception>
    internal async Task<string?> Replace(DirectTarget target, DirectTokens tokens, DirectTokenProvider previous, CancellationToken cancellationToken, Action? publish = null)
    {
        IAsyncDisposable held;
        try
        {
            held = await refreshLock.Acquire(target.Key, cancellationToken);
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            var recovery = await RevokeQuietly(tokens.RefreshToken);
            if (recovery is not null)
            {
                throw new DirectAuthError("The new Direct login could not be saved. " + recovery);
            }

            throw;
        }

        await using var lease = held;
        DirectTokens? old;
        string? previousWarning = null;
        try
        {
            old = await previous.Read(target, cancellationToken);
        }
        catch (DirectAuthError ex) when (ex.InvalidCredential && !cancellationToken.IsCancellationRequested)
        {
            old = null;
            previousWarning = "The previous Direct credential was unreadable and could not be revoked.";
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            var recovery = await RevokeQuietly(tokens.RefreshToken);
            if (recovery is not null)
            {
                throw new DirectAuthError("The new Direct login could not be saved. " + recovery);
            }

            throw;
        }

        var sameStore = previous.UsesStore(store.GetType());
        var saved = false;
        try
        {
            await Save(target, tokens, cancellationToken);
            saved = true;
            publish?.Invoke();
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            var recovery = await RevokeQuietly(tokens.RefreshToken);
            try
            {
                // A canceled or failed store operation may already have committed. Its owned process has
                // stopped before returning; reconcile under the lock before publishing or releasing it.
                if (saved || await store.Read(target.Key, CancellationToken.None) == JsonSerializer.Serialize(tokens with { Issuer = authorizationIssuer.OriginalString }))
                {
                    if (sameStore && old is not null)
                    {
                        await store.Write(target.Key, JsonSerializer.Serialize(old), CancellationToken.None);
                    }
                    else
                    {
                        await store.Delete(target.Key, CancellationToken.None);
                    }
                }
            }
            catch (Exception cleanup) when (DirectLoginFlow.IsSafeFailure(cleanup))
            {
                recovery = (recovery is null ? string.Empty : recovery + " ") + "The new Direct credential could not be removed or the previous credential restored; check the credential store.";
            }

            if (recovery is not null)
            {
                throw new DirectAuthError("The new Direct login could not be saved. " + recovery);
            }

            throw;
        }

        // Cancellation after publication must not undo the working login or leave the previous token unattempted.
        var warning = old is null ? previousWarning : await previous.RevokePrevious(old, CancellationToken.None);
        return sameStore ? warning : await previous.Remove(target, warning, CancellationToken.None);
    }

    static bool IsRecoverable(Exception ex, CancellationToken cancellationToken) =>
        DirectLoginFlow.IsSafeFailure(ex) && !cancellationToken.IsCancellationRequested;

    static bool IsUsable(DirectTokens tokens, string? rejected) =>
        !string.IsNullOrEmpty(tokens.AccessToken) && tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1) &&
        (rejected is null || !string.Equals(tokens.AccessToken, rejected, StringComparison.Ordinal));

    bool UsesStore(Type type) => store.GetType() == type;

    async Task<string?> RevokePrevious(DirectTokens tokens, CancellationToken cancellationToken)
    {
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

        return failure;
    }

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

    async Task<string?> RevokeQuietly(string refreshToken)
    {
        try
        {
            // Not canceled with the login, but bounded by the HTTP deadline.
            var status = await PostRevocation(refreshToken, CancellationToken.None);
            return status is null ? null : "The new Direct refresh token could not be revoked; revoke it at the authorization server.";
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            return "The new Direct refresh token could not be revoked; revoke it at the authorization server.";
        }
    }

    async Task<string> Refresh(DirectTarget target, string? rejected, CancellationToken cancellationToken)
    {
        // Re-read after the lock: another process may already have rotated the refresh token.
        await using var held = await refreshLock.Acquire(target.Key, cancellationToken);
        var tokens = await Read(target, cancellationToken) ?? throw new DirectAuthError("Direct session was logged out. Run 'cratis direct login'.");
        if (IsUsable(tokens, rejected))
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

        // The server may have consumed the previous refresh token. Finish persistence while still holding the lock.
        await Save(target, refreshed, CancellationToken.None);
        return refreshed.AccessToken;
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
        using var deadline = DirectHttp.Deadline(http, cancellationToken);
        cancellationToken = deadline.Token;
        var endpoints = await discovery.DiscoverIssuer(authorizationIssuer, cancellationToken);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken, ["token_type_hint"] = "refresh_token", ["client_id"] = "cratis-cli"
        });
        using var response = await http.PostAsync(endpoints.Revocation, form, cancellationToken);
        return response.IsSuccessStatusCode ? null : (int)response.StatusCode;
    }
}
