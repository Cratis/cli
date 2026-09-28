// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Net;
using System.Net.Sockets;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Common Direct login options.</summary>
public class DirectSettings : GlobalSettings
{
    /// <summary>Gets or sets the Direct HTTPS origin.</summary>
    [CommandOption("--url <ORIGIN>")]
    [Description("Direct origin (default: https://cratis.direct)")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the authorization server issuer when protected-resource metadata is unavailable.</summary>
    [CommandOption("--issuer <URL>")]
    [Description("Authorization server issuer (HTTPS; plain HTTP only on localhost); required if Direct does not publish resource metadata")]
    public string? Issuer { get; set; }

    /// <summary>Gets or sets explicit consent to store tokens in 0600 plaintext files instead of the OS credential manager.</summary>
    [CommandOption("--insecure-file-store")]
    [Description("Explicitly use owner-only plaintext files if the OS credential manager is unavailable (Unix only)")]
    public bool InsecureFileStore { get; set; }
}

/// <summary>Options for a new Direct login.</summary>
public sealed class DirectLoginSettings : DirectSettings
{
    /// <summary>Gets or sets the tenant hint sent to the authorization server.</summary>
    [CommandOption("--tenant <TENANT>")]
    [Description("Tenant hint passed to the authorization server (not proof of membership)")]
    public string? Tenant { get; set; }
}

/// <summary>Options for switching tenant via re-authorization.</summary>
public sealed class DirectUseSettings : DirectSettings
{
    /// <summary>Gets or sets the new tenant hint.</summary>
    [CommandArgument(0, "<TENANT>")]
    [Description("Tenant to authorize as; a new browser login is required")]
    public string Tenant { get; set; } = string.Empty;
}

/// <summary>Options for revoking stored Direct credentials.</summary>
public sealed class DirectLogoutSettings : DirectSettings
{
    /// <summary>Gets or sets the tenant whose stored credential to revoke.</summary>
    [CommandOption("--tenant <TENANT>")]
    [Description("Revoke the stored credential for this tenant instead of the active one")]
    public string? Tenant { get; set; }

    /// <summary>Gets or sets whether to revoke every stored Direct credential.</summary>
    [CommandOption("--all")]
    [Description("Revoke and delete every stored Direct credential, on all origins unless --url is given")]
    public bool All { get; set; }
}

/// <summary>Signs in via an external browser and stores the tokens outside CLI config.</summary>
[LlmDescription("Sign in to Direct with an external browser using OAuth authorization code and PKCE. Tokens stay in the OS keychain, separate from Chronicle contexts. A previous credential for the same origin and tenant is revoked at the authorization server and replaced.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("login", "Sign in to Direct using your browser", Branch = typeof(DirectBranch))]
[CliExample("direct", "login", "--tenant", "my-tenant")]
[LlmOption("--url", "string", "Direct HTTPS origin (default https://cratis.direct)")]
[LlmOption("--issuer", "string", "Authorization server issuer when resource metadata is missing")]
[LlmOption("--tenant", "string", "Optional tenant hint for the authorization server")]
public sealed class DirectLoginCommand : AsyncCommand<DirectLoginSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, DirectLoginSettings settings, CancellationToken cancellationToken) =>
        DirectLoginFlow.Execute(settings, settings.Tenant, cancellationToken);
}

/// <summary>Reauthorizes in the browser for a different Direct tenant.</summary>
[LlmDescription("Reauthorize your Direct CLI login for another tenant. A tenant hint cannot switch an existing token's authority. A previous credential for that same tenant is revoked and replaced; other tenants' credentials stay stored.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("use", "Reauthorize for a Direct tenant", Branch = typeof(DirectBranch))]
[CliExample("direct", "use", "another-tenant")]
[LlmOption("<TENANT>", "string", "Tenant hint; authorization server checks membership")]
public sealed class DirectUseCommand : AsyncCommand<DirectUseSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, DirectUseSettings settings, CancellationToken cancellationToken) =>
        DirectLoginFlow.Execute(settings, settings.Tenant, cancellationToken);
}

/// <summary>Shared login execution and explicit store selection.</summary>
internal static class DirectLoginFlow
{
    static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    internal static HttpClient CreateHttp() => new(new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true }) { Timeout = TimeSpan.FromSeconds(15) };

    internal static async Task<int> Execute(DirectSettings settings, string? tenant, CancellationToken cancellationToken)
    {
        try
        {
            var config = CliConfiguration.Load();
            var previous = config.Direct;
            var target = DirectTarget.Create(settings.Url ?? previous?.Origin ?? "https://cratis.direct", tenant ?? previous?.Tenant);
            using var http = CreateHttp();
            var discovery = new DirectDiscovery(http);
            var issuerHint = settings.Issuer ?? (previous?.Origin == target.Origin.GetLeftPart(UriPartial.Authority) ? previous.Issuer : null);
            var endpoints = await discovery.Discover(target, issuerHint, cancellationToken);
            var store = DirectSecretStores.Select(UseInsecureFileStore(settings, previous, target), Home);
            var provider = new DirectTokenProvider(store, new DirectRefreshLock(Home), discovery, http, endpoints.Issuer);
            var (code, verifier, redirect) = await new DirectBrowser().Authorize(endpoints, target, cancellationToken);
            var tokens = await provider.Exchange(
            endpoints.Token,
            target,
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = redirect.AbsoluteUri,
                ["client_id"] = "cratis-cli", ["code_verifier"] = verifier
            },
            null,
            cancellationToken);

            // Revoke the credential being replaced; it may live under another issuer or store.
            var supersedeWarning = await provider.Replace(target, tokens, Superseding(previous, target, provider, http), cancellationToken);

            var selection = previous ?? new DirectConfiguration();
            selection.Origin = DirectCredentials.OriginOf(target);
            selection.Tenant = target.Tenant;
            selection.Issuer = endpoints.Issuer.OriginalString;
            selection.InsecureFileStore = store is DirectFileSecrets;
            DirectCredentials.Record(selection, new DirectCredentialEntry
            {
                Origin = selection.Origin, Tenant = target.Tenant, Issuer = selection.Issuer, InsecureFileStore = selection.InsecureFileStore
            });
            config.Direct = selection;
            config.Save();
            if (supersedeWarning is not null)
            {
                await Console.Error.WriteLineAsync($"Warning: {supersedeWarning}");
            }

            OutputFormatter.WriteMessage(settings.ResolveOutputFormat(), $"Logged in to Direct{(target.Tenant is null ? string.Empty : $" for tenant '{target.Tenant}'")}.");
            return ExitCodes.Success;
        }
        catch (Exception ex) when (IsSafeFailure(ex))
        {
            return Fail(settings, ex);
        }
    }

    internal static bool UseInsecureFileStore(DirectSettings settings, DirectConfiguration? previous, DirectTarget target) =>
        settings.InsecureFileStore || DirectCredentials.Find(previous, target)?.InsecureFileStore == true || (previous?.InsecureFileStore == true &&
            previous.Origin == target.Origin.GetLeftPart(UriPartial.Authority) && previous.Tenant == target.Tenant);

    /// <summary>Creates the token provider for a stored credential, using the store recorded for that credential only.</summary>
    /// <param name="entry">The stored credential.</param>
    /// <param name="http">HTTP transport.</param>
    /// <param name="stores">Selects the secret store by whether plaintext file storage was chosen; the OS or file store by default.</param>
    /// <returns>The token provider.</returns>
    /// <exception cref="DirectAuthError">When the recorded issuer is invalid.</exception>
    internal static DirectTokenProvider ProviderFor(DirectCredentialEntry entry, HttpClient http, Func<bool, IDirectSecretStore>? stores = null)
    {
        if (!Uri.TryCreate(entry.Issuer, UriKind.Absolute, out var issuer) || !DirectIssuerScheme.IsAllowed(issuer))
        {
            throw new DirectAuthError("The stored Direct issuer is invalid, so the credential cannot be revoked.");
        }

        var store = stores is null ? DirectSecretStores.Select(entry.InsecureFileStore, Home) : stores(entry.InsecureFileStore);
        return new DirectTokenProvider(store, new DirectRefreshLock(Home), new DirectDiscovery(http), http, issuer);
    }

    internal static (DirectTarget Target, Uri Issuer, DirectTokenProvider Provider) Active(CliConfiguration config, DirectSettings settings, HttpClient http)
    {
        var selected = config.Direct ?? throw new DirectAuthError("Not logged in to Direct. Run 'cratis direct login'.");
        var target = DirectTarget.Create(settings.Url ?? selected.Origin, selected.Tenant);
        if (target.Origin.GetLeftPart(UriPartial.Authority) != selected.Origin ||
            (settings.Issuer is not null && settings.Issuer != selected.Issuer))
        {
            throw new DirectAuthError("This Direct origin or issuer is not active. Run 'cratis direct login' first.");
        }

        if (selected.Issuer is null || !Uri.TryCreate(selected.Issuer, UriKind.Absolute, out var issuer) || !DirectIssuerScheme.IsAllowed(issuer))
        {
            throw new DirectAuthError("Direct issuer is missing. Run 'cratis direct login' again.");
        }
        var store = DirectSecretStores.Select(settings.InsecureFileStore || selected.InsecureFileStore, Home);
        return (target, issuer, new DirectTokenProvider(store, new DirectRefreshLock(Home), new DirectDiscovery(http), http, issuer));
    }

    internal static string? Property(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    internal static bool IsSafeFailure(Exception ex) => ex is DirectAuthError or HttpRequestException or HttpListenerException or SocketException or Win32Exception or IOException or UnauthorizedAccessException or JsonException or OperationCanceledException;

    internal static int Fail(DirectSettings settings, Exception ex)
    {
        var message = ex switch
        {
            DirectAuthError auth => auth.Message,
            OperationCanceledException => "Direct request timed out or was canceled.",
            _ => "Direct authentication could not complete; check the network, credential manager, and CLI configuration."
        };
        OutputFormatter.WriteError(settings.ResolveOutputFormat(), "Direct authentication failed", message, ExitCodes.AuthenticationErrorCode);
        return ExitCodes.AuthenticationError;
    }

    static DirectTokenProvider Superseding(DirectConfiguration? previous, DirectTarget target, DirectTokenProvider current, HttpClient http)
    {
        if (DirectCredentials.Find(previous, target) is not { } entry)
        {
            return current;
        }

        try
        {
            return ProviderFor(entry, http);
        }
        catch (DirectAuthError)
        {
            return current;
        }
    }
}
