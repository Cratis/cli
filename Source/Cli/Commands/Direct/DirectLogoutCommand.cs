// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Revokes stored Direct refresh tokens before removing local credentials.</summary>
[LlmDescription("Revoke Direct refresh tokens at the authorization server and delete the local credentials: the active origin and tenant by default, another tenant with --tenant, or every stored credential with --all.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("logout", "Revoke stored Direct logins", Branch = typeof(DirectBranch))]
[CliExample("direct", "logout")]
[CliExample("direct", "logout", "--tenant", "old-tenant")]
[CliExample("direct", "logout", "--all")]
[LlmOption("--tenant", "string", "Revoke the stored credential for this tenant on the selected origin")]
[LlmOption("--all", "bool", "Revoke every stored Direct credential, on all origins unless --url restricts it")]
public sealed class DirectLogoutCommand : AsyncCommand<DirectLogoutSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, DirectLogoutSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var config = CliConfiguration.Load();
            var direct = config.Direct;
            var targets = direct is null ? [] : DirectCredentials.Select(direct, settings.Url, settings.Tenant, settings.All);
            if (direct is null || targets.Count == 0)
            {
                OutputFormatter.WriteMessage(format, settings.All ? "No stored Direct credentials." : "No stored Direct credential for this origin and tenant.");
                return ExitCodes.Success;
            }

            using var http = DirectLoginFlow.CreateHttp();
            var outcomes = await DirectCredentials.Logout(direct, targets, entry => DirectLoginFlow.ProviderFor(entry, settings.InsecureFileStore, http), cancellationToken);
            config.Save();
            foreach (var outcome in outcomes.Where(outcome => outcome.Failure is null))
            {
                var described = DirectCredentials.Describe(outcome.Credential);
                OutputFormatter.WriteMessage(format, outcome.Revoked ? $"Logged out of Direct ({described})." : $"No stored Direct credential for {described}.");
            }

            var failures = outcomes.Where(outcome => outcome.Failure is not null).ToArray();
            if (failures.Length == 0)
            {
                return ExitCodes.Success;
            }

            OutputFormatter.WriteError(
                format,
                "Direct logout was incomplete",
                string.Join(' ', failures.Select(failure => $"{DirectCredentials.Describe(failure.Credential)}: {failure.Failure}")),
                ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            return DirectLoginFlow.Fail(settings, ex);
        }
    }
}
