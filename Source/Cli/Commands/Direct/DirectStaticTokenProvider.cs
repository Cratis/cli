// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Reads a personal access token without issuer discovery or refresh.</summary>
/// <param name="store">The recorded credential store.</param>
internal sealed class DirectStaticTokenProvider(IDirectSecretStore store) : IDirectTokenProvider
{
    internal const string Rejected = "token invalid or expired - create a new personal access token in Direct";

    public async Task<string> GetAccessToken(DirectTarget target, Uri issuer, CancellationToken cancellationToken)
    {
        var json = await store.Read(target.Key, cancellationToken) ?? throw new DirectAuthError("Not logged in to Direct. Run 'cratis direct login --token'.");
        try
        {
            var tokens = JsonSerializer.Deserialize<DirectTokens>(json);
            if (tokens?.Kind != DirectCredentialEntry.PersonalAccessToken || !DirectPersonalAccessLogin.IsValid(tokens.AccessToken))
            {
                throw new DirectAuthError("Stored Direct personal access token is invalid.");
            }
            return tokens.AccessToken;
        }
        catch (JsonException)
        {
            throw new DirectAuthError("Stored Direct personal access token is invalid.");
        }
    }

    public Task<string> RefreshAccessToken(DirectTarget target, Uri issuer, string rejected, CancellationToken cancellationToken) =>
        Task.FromException<string>(new DirectAuthError(Rejected));
}
