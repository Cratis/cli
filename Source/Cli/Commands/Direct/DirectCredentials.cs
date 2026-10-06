// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>The outcome of logging out one stored Direct credential.</summary>
/// <param name="Credential">The credential's non-secret metadata.</param>
/// <param name="Revoked">Whether a stored refresh token was revoked and deleted.</param>
/// <param name="Failure">A message without credentials when revocation failed and the credential was retained.</param>
internal sealed record DirectLogoutOutcome(DirectCredentialEntry Credential, bool Revoked, string? Failure);

/// <summary>Maintains the non-secret index of stored Direct credentials and logs them out.</summary>
internal static class DirectCredentials
{
    internal static string OriginOf(DirectTarget target) => target.Origin.GetLeftPart(UriPartial.Authority);

    internal static DirectCredentialEntry? Find(DirectConfiguration? config, DirectTarget target) =>
        config?.Credentials.FirstOrDefault(entry => Matches(entry, OriginOf(target), target.Tenant));

    internal static void Record(DirectConfiguration config, DirectCredentialEntry entry)
    {
        RemoveEntry(config, entry);
        config.Credentials.Add(entry);
    }

    internal static void Forget(DirectConfiguration config, DirectCredentialEntry entry)
    {
        RemoveEntry(config, entry);
        if (config.Origin == entry.Origin && config.Tenant == entry.Tenant)
        {
            config.Origin = new DirectConfiguration().Origin;
            config.Tenant = null;
            config.Issuer = null;
            config.InsecureFileStore = false;
        }
    }

    /// <summary>Selects the credentials a logout addresses.</summary>
    /// <param name="config">The Direct configuration.</param>
    /// <param name="url">An explicit origin, or null for the active origin (or every origin with <paramref name="all"/>).</param>
    /// <param name="tenant">An explicit tenant, or null for the active tenant.</param>
    /// <param name="all">Whether to select every stored credential, restricted to <paramref name="url"/> when given.</param>
    /// <returns>The selected credentials; empty when none is stored.</returns>
    /// <exception cref="DirectAuthError">When both --all and --tenant are given, or the origin or tenant is invalid.</exception>
    internal static IReadOnlyList<DirectCredentialEntry> Select(DirectConfiguration config, string? url, string? tenant, bool all)
    {
        if (all && tenant is not null)
        {
            throw new DirectAuthError("Use either --all or --tenant, not both.");
        }

        if (all)
        {
            var origin = url is null ? null : OriginOf(DirectTarget.Create(url, null));
            return [.. config.Credentials.Where(entry => origin is null || entry.Origin == origin)];
        }

        var target = DirectTarget.Create(url ?? config.Origin, tenant ?? config.Tenant);
        return Find(config, target) is { } found ? [found] : [];
    }

    /// <summary>Revokes and deletes each selected credential, continuing past failures, and forgets those no longer stored.</summary>
    /// <param name="config">The Direct configuration whose index is updated.</param>
    /// <param name="targets">The credentials to log out.</param>
    /// <param name="providers">Creates the token provider for a credential's store and issuer.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="localRemoval">Explicit local-only recovery, without reading or revoking tokens.</param>
    /// <returns>One outcome per credential.</returns>
    internal static async Task<IReadOnlyList<DirectLogoutOutcome>> Logout(
        DirectConfiguration config,
        IReadOnlyList<DirectCredentialEntry> targets,
        Func<DirectCredentialEntry, DirectTokenProvider> providers,
        CancellationToken cancellationToken,
        Func<DirectCredentialEntry, CancellationToken, Task>? localRemoval = null)
    {
        var outcomes = new List<DirectLogoutOutcome>();
        foreach (var entry in targets)
        {
            try
            {
                var revoked = false;
                if (localRemoval is not null)
                {
                    await localRemoval(entry, cancellationToken);
                }
                else
                {
                    revoked = await providers(entry).Revoke(DirectTarget.Create(entry.Origin, entry.Tenant), cancellationToken);
                }

                Forget(config, entry);
                outcomes.Add(new(entry, revoked, null));
            }
            catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex) && !cancellationToken.IsCancellationRequested)
            {
                var failure = ex is DirectAuthError ? ex.Message : "The authorization server or credential store could not be reached; local credentials were retained.";
                outcomes.Add(new(entry, false, failure));
            }
        }

        return outcomes;
    }

    internal static string Describe(DirectCredentialEntry entry) =>
        $"{new Uri(entry.Origin).Host}{(entry.Tenant is null ? string.Empty : $", tenant '{entry.Tenant}'")}";

    static void RemoveEntry(DirectConfiguration config, DirectCredentialEntry entry)
    {
        for (var index = config.Credentials.Count - 1; index >= 0; index--)
        {
            if (Matches(config.Credentials[index], entry.Origin, entry.Tenant))
            {
                config.Credentials.RemoveAt(index);
            }
        }
    }

    static bool Matches(DirectCredentialEntry entry, string origin, string? tenant) =>
        entry.Origin == origin && string.Equals(entry.Tenant, tenant, StringComparison.Ordinal);
}
