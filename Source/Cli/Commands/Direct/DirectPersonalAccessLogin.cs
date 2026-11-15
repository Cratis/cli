// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Stores a personal access token using the existing credential replacement transaction.</summary>
internal static class DirectPersonalAccessLogin
{
    internal static bool IsValid(string? token) => token?.StartsWith("dpat_", StringComparison.Ordinal) == true &&
        token.Length > 5 && token.Skip(5).All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    internal static void ValidateOptions(string? tenant, string? issuer)
    {
        if (tenant is not null) throw new DirectAuthError("--tenant cannot be used with --token; the tenant and organization are implied by the token.");
        if (issuer is not null) throw new DirectAuthError("--issuer cannot be used with --token; personal access tokens have no issuer.");
    }

    internal static async Task<string> Read(string? value, TextReader input, Func<string?> environment, CancellationToken cancellationToken)
    {
        var token = value == "-" ? null : value ?? environment();
        token ??= await input.ReadLineAsync(cancellationToken);
        if (!IsValid(token)) throw new DirectAuthError("Invalid personal access token format; expected 'dpat_' followed by letters, digits, '_' or '-'.");
        return token!;
    }

    internal static async Task<int> Execute(DirectTokenSettings settings, string? tenant, CancellationToken cancellationToken, bool report = true)
    {
        try
        {
            ValidateOptions(tenant, settings.Issuer);
            var token = await Read(settings.Token.Value, Console.In, () => Environment.GetEnvironmentVariable("DIRECT_TOKEN"), cancellationToken);
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            using var http = DirectLoginFlow.CreateHttp();
            var configurations = new DirectConfigurationStore(new DirectRefreshLock(home));
            var warning = await configurations.Update(
                async (config, save) =>
            {
                var target = DirectTarget.Create(settings.Url ?? config.Direct?.Origin ?? "https://app.cratis.direct", null);
                var entry = new DirectCredentialEntry
                {
                    Origin = DirectCredentials.OriginOf(target),
                    Kind = DirectCredentialEntry.PersonalAccessToken,
                    InsecureFileStore = DirectLoginFlow.UseInsecureFileStore(settings, config.Direct, target)
                };
                return await Store(config, entry, token, http, save, cancellationToken);
            },
                cancellationToken);
            if (warning is not null) await Console.Error.WriteLineAsync($"Warning: {warning}");
            if (report) OutputFormatter.WriteMessage(settings.ResolveOutputFormat(), "Logged in to Direct using a personal access token.");
            return ExitCodes.Success;
        }
        catch (Exception ex) when (report && DirectLoginFlow.IsSafeFailure(ex))
        {
            return DirectLoginFlow.Fail(settings, ex);
        }
    }

    internal static Task<string?> Store(CliConfiguration config, DirectCredentialEntry entry, string token, HttpClient http, Action save, CancellationToken cancellationToken, Func<bool, IDirectSecretStore>? stores = null)
    {
        var target = DirectTarget.Create(entry.Origin, null);
        var provider = DirectLoginFlow.ProviderFor(entry, http, stores);
        var previous = DirectCredentials.Find(config.Direct, target) is { } old ? DirectLoginFlow.ProviderFor(old, http, stores) : provider;
        var tokens = new DirectTokens(token, null, DateTimeOffset.MaxValue, string.Empty, Kind: DirectCredentialEntry.PersonalAccessToken);
        return provider.Replace(target, tokens, previous, cancellationToken, () =>
        {
            var original = config.Direct;
            var selection = original is null ? new DirectConfiguration() : JsonSerializer.Deserialize<DirectConfiguration>(JsonSerializer.Serialize(original))!;
            selection.Origin = entry.Origin;
            selection.Tenant = null;
            selection.Issuer = null;
            selection.InsecureFileStore = entry.InsecureFileStore;
            DirectCredentials.Record(selection, entry);
            config.Direct = selection;
            try
            {
                save();
            }
            catch
            {
                config.Direct = original;
                throw;
            }
        });
    }
}
