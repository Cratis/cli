// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Non-secret setup outcome for one client.</summary>
/// <param name="Client">The client name.</param>
/// <param name="State">The registration outcome.</param>
/// <param name="Reason">The reason for a refusal.</param>
internal sealed record DirectSetupClientResult(string Client, string State, string? Reason = null);

/// <summary>Non-secret setup summary.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Login">The non-secret login outcome.</param>
/// <param name="Clients">The per-client outcomes.</param>
internal sealed record DirectSetupResult(int ExitCode, string Login, IReadOnlyList<DirectSetupClientResult> Clients);

/// <summary>Composes existing login and registration flows without owning credentials or client file formats.</summary>
internal static class DirectSetup
{
    internal static async Task<DirectSetupResult> Run(
        DirectSetupSettings settings,
        DirectMcpLocations locations,
        DirectConfiguration? config,
        Func<DirectTarget, CancellationToken, Task<bool>> hasLogin,
        Func<DirectLoginSettings, CancellationToken, Task<int>> login,
        CancellationToken cancellationToken)
    {
        var scope = new DirectMcpRegistrationSettings { Scope = settings.Scope }.SelectedScope;
        var clients = DirectSetupClients.Select(settings.Clients, locations);
        if (clients.Count == 0)
        {
            return new(ExitCodes.ValidationError, "No MCP clients detected. Select clients with --client, for example --client claude,codex.", []);
        }

        var target = DirectLoginFlow.TargetFor(settings, config, settings.Tenant);
        var loginMessage = "Would check the Direct login and open a browser only if login is needed.";
        if (!settings.DryRun)
        {
            var valid = await hasLogin(target, cancellationToken);
            if (!valid)
            {
                var loginSettings = new DirectLoginSettings
                {
                    Url = DirectCredentials.OriginOf(target),
                    Tenant = target.Tenant,
                    Issuer = settings.Issuer,
                    InsecureFileStore = settings.InsecureFileStore,
                    Output = settings.Output,
                    Quiet = true
                };
                var code = await login(loginSettings, cancellationToken);
                if (code != ExitCodes.Success) return new(code, "Direct login did not complete; no clients were registered.", []);
            }
            loginMessage = valid ? "Using the existing Direct login." : "Direct login completed.";
        }

        var results = new List<DirectSetupClientResult>();
        foreach (var client in clients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(Register(client, settings, locations, scope, target));
        }
        var exit = results.Exists(client => client.State == "refused") ? ExitCodes.ValidationError : ExitCodes.Success;
        return new(exit, loginMessage, results);
    }

    static DirectSetupClientResult Register(string client, DirectSetupSettings settings, DirectMcpLocations locations, DirectMcpScope scope, DirectTarget target)
    {
        try
        {
            var args = DirectMcpClients.Arguments(DirectCredentials.OriginOf(target), target.Tenant);
            var plan = DirectMcpRegistration.Install(scope, locations, [client], args);
            if (plan.Unsupported.Count != 0) return new(client, "refused", string.Join("; ", plan.Unsupported));
            if (plan.Conflicts.Count != 0) return new(client, "refused", string.Join("; ", plan.Conflicts));
            plan.Apply(new(settings.DryRun));
            var state = plan.Changes.Count == 0 ? "already configured" : "installed";
            if (settings.DryRun && plan.Changes.Count != 0) state = "would install";
            return new(client, state);
        }
        catch (Exception ex) when (ex is AiMcpConfigurationInvalid || DirectLoginFlow.IsSafeFailure(ex))
        {
            return new(client, "refused", ex.Message);
        }
    }
}
