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
[LlmOption("--local", "bool", "Delete local credentials without revocation; server-side tokens may remain valid")]
public sealed class DirectLogoutCommand : AsyncCommand<DirectLogoutSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, DirectLogoutSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            using var http = DirectLoginFlow.CreateHttp();
            var configurations = new DirectConfigurationStore(new DirectRefreshLock(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
            var storedCredentials = 0;
            var noActiveLogin = false;
            var outcomes = await configurations.Update<IReadOnlyList<DirectLogoutOutcome>>(
            async (config, save) =>
            {
                var direct = config.Direct;
                storedCredentials = direct?.Credentials.Count ?? 0;
                noActiveLogin = settings.Url is null && settings.Tenant is null && !settings.All && direct?.HasActiveSelection != true;
                var targets = direct is null ? [] : DirectCredentials.Select(direct, settings.Url, settings.Tenant, settings.All);
                if (direct is null || targets.Count == 0)
                {
                    return [];
                }

                var results = await DirectCredentials.Logout(
                    direct,
                    targets,
                    entry => DirectLoginFlow.ProviderFor(entry, http),
                    cancellationToken,
                    settings.Local ? (entry, token) => DirectLoginFlow.ForgetLocally(entry, token) : null);
                save();
                return results;
            },
            cancellationToken);
            if (outcomes.Count == 0)
            {
                OutputFormatter.WriteMessage(format, NothingToLogOutOf(settings.All, noActiveLogin, storedCredentials));
                return ExitCodes.Success;
            }

            foreach (var outcome in outcomes.Where(outcome => outcome.Failure is null))
            {
                var described = DirectCredentials.Describe(outcome.Credential);
                var message = outcome.Revoked ? $"Logged out of Direct ({described})." : $"No stored Direct credential for {described}.";
                if (settings.Local)
                {
                    message = $"Local Direct credential removed ({described}); server-side revocation was not performed. Tokens may remain valid at the authorization server.";
                }

                OutputFormatter.WriteMessage(format, message);
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

    /// <summary>
    /// Describes why a logout addressed no credential.
    /// </summary>
    /// <param name="all">Whether every credential was addressed.</param>
    /// <param name="noActiveLogin">Whether an untargeted logout ran without an active login.</param>
    /// <param name="storedCredentials">The number of stored Direct credentials.</param>
    /// <returns>The message to show.</returns>
    internal static string NothingToLogOutOf(bool all, bool noActiveLogin, int storedCredentials)
    {
        if (all)
        {
            return "No stored Direct credentials.";
        }

        if (!noActiveLogin)
        {
            return "No stored Direct credential for this origin and tenant.";
        }

        if (storedCredentials == 0)
        {
            return "Not logged in to Direct.";
        }

        return $"No Direct login is active. {storedCredentials} stored Direct credential(s) remain; revoke them with 'cratis direct logout --tenant <TENANT>', '--url <URL>' or '--all'.";
    }
}
