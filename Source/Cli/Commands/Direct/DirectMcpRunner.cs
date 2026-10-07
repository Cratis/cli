// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Pins the Direct target and stored credential from the CLI configuration, then runs the bridge.</summary>
internal sealed class DirectMcpRunner : IDirectMcpRunner
{
    /// <inheritdoc/>
    public async Task Run(DirectMcpOptions options, TextReader input, TextWriter output, TextWriter log, CancellationToken cancellationToken)
    {
        var (target, credential) = Resolve(CliConfiguration.Load().Direct, options);
        using var authorization = DirectLoginFlow.CreateHttp();
        var provider = DirectLoginFlow.ProviderFor(credential, authorization);

        // No timeout: tool calls may stream for as long as the client waits, and the client cancels them itself.
        using var mcp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true }) { Timeout = Timeout.InfiniteTimeSpan };
        await log.WriteLineAsync($"Direct MCP: forwarding to {target.Resource}{(target.Tenant is null ? string.Empty : $" for tenant '{target.Tenant}'")}.");
        using var bridge = new DirectMcpBridge(mcp, provider, target, new Uri(credential.Issuer), log);
        await bridge.Run(input, output, cancellationToken);
    }

    /// <summary>
    /// Resolves the origin and tenant to pin: explicit options first, otherwise the active Direct login. The active
    /// login's tenant is inherited only when neither an origin nor a tenant pin was given, so a registration that pins
    /// an origin without a tenant keeps running without one after 'cratis direct use' selects a tenant.
    /// </summary>
    /// <param name="config">The Direct configuration.</param>
    /// <param name="options">The requested origin and tenant.</param>
    /// <returns>The target and the stored credential that serves it.</returns>
    /// <exception cref="DirectAuthError">When no stored credential serves the target.</exception>
    internal static (DirectTarget Target, DirectCredentialEntry Credential) Resolve(DirectConfiguration? config, DirectMcpOptions options)
    {
        if (config is null || (options.Url is null && !options.PinsTenant && !config.HasActiveSelection))
        {
            throw new DirectAuthError("Not logged in to Direct. Run 'cratis direct login'.");
        }

        var origin = options.Url ?? config.Origin;
        var inherit = options.Url is null && !options.PinsTenant;
        var target = DirectTarget.Create(origin, inherit ? config.Tenant : options.Tenant);
        var credential = DirectCredentials.Find(config, target) ?? Legacy(config, target);
        if (credential is null)
        {
            var login = $"cratis direct login{(options.Url is null ? string.Empty : $" --url {origin}")}{(target.Tenant is null ? string.Empty : $" --tenant {target.Tenant}")}";
            throw new DirectAuthError($"No stored Direct login for {DirectCredentials.OriginOf(target)}{(target.Tenant is null ? string.Empty : $", tenant '{target.Tenant}'")}. Run '{login}'.");
        }

        return (target, credential);
    }

    /// <summary>Describes a credential stored before the credential index existed, which only the active selection records.</summary>
    /// <param name="config">The Direct configuration.</param>
    /// <param name="target">The target.</param>
    /// <returns>The credential, or null when the active selection does not describe the target.</returns>
    static DirectCredentialEntry? Legacy(DirectConfiguration config, DirectTarget target) =>
        config.Issuer is not null && config.Origin == DirectCredentials.OriginOf(target) && config.Tenant == target.Tenant
            ? new DirectCredentialEntry { Origin = config.Origin, Tenant = config.Tenant, Issuer = config.Issuer, InsecureFileStore = config.InsecureFileStore }
            : null;
}
