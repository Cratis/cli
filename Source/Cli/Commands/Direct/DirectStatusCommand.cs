// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Checks the live Direct identity endpoint and lists stored credential targets; never exposes token values.</summary>
[LlmDescription("Show the signed-in Direct user, tenant, granted scopes and token expiry by calling Direct's identity endpoint, and list the origin and tenant of every stored Direct credential.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("status", "Show the current Direct login status", Branch = typeof(DirectBranch))]
[CliExample("direct", "status", "-o", "json")]
public sealed class DirectStatusCommand : AsyncCommand<DirectSettings>
{
    /// <summary>Lists stored credential targets as non-secret metadata, marking the active one.</summary>
    /// <param name="config">The Direct configuration.</param>
    /// <param name="active">The active target.</param>
    /// <returns>The stored credential targets.</returns>
    internal static IReadOnlyList<DirectStoredCredential> StoredCredentials(DirectConfiguration config, DirectTarget active) =>
        [.. config.Credentials
            .Select(entry => new DirectStoredCredential(
                entry.Origin,
                entry.Tenant,
                entry.InsecureFileStore ? "file" : "os",
                entry.Origin == DirectCredentials.OriginOf(active) && entry.Tenant == active.Tenant))];

    internal static string NotLoggedIn(int storedCredentials) => storedCredentials == 0
        ? "Not logged in to Direct. Run 'cratis direct login'."
        : $"Not logged in to Direct for the selected origin and tenant. {storedCredentials} other stored Direct credential(s) exist; select one with 'cratis direct use <TENANT>' or revoke them with 'cratis direct logout --all'.";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, DirectSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var config = CliConfiguration.Load();
            using var http = DirectLoginFlow.CreateHttp();
            var (target, issuer, provider) = DirectLoginFlow.Active(config, settings, http);
            if (await provider.Read(target, cancellationToken) is null)
            {
                throw new DirectAuthError(NotLoggedIn(StoredCredentials(config.Direct!, target).Count(credential => !credential.Active)));
            }

            var token = await provider.GetAccessToken(target, issuer, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(target.Origin, "/.cratis/me"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new DirectAuthError("Direct refused this session (401). Run 'cratis direct login' again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new DirectAuthError($"Direct identity endpoint returned HTTP {(int)response.StatusCode}.");
            }

            using var document = await ReadIdentity(response, cancellationToken);
            var root = document.RootElement;
            var details = root.TryGetProperty("details", out var inner) && inner.ValueKind == JsonValueKind.Object ? inner : root;
            var name = DirectLoginFlow.Property(details, "login") ?? DirectLoginFlow.Property(details, "name") ?? DirectLoginFlow.Property(root, "name") ?? "(unknown)";
            var tokens = await provider.Read(target, cancellationToken) ?? throw new DirectAuthError("Direct session was removed.");
            var status = new DirectStatus(
                name,
                DirectLoginFlow.Property(root, "tenant") ?? DirectLoginFlow.Property(root, "tenantId") ?? target.Tenant ?? "(unspecified)",
                tokens.Scopes,
                tokens.ExpiresAt,
                StoredCredentials(config.Direct!, target));
            OutputFormatter.WriteObject(settings.ResolveOutputFormat(), status, Render);
            return ExitCodes.Success;
        }
        catch (Exception ex) when (DirectLoginFlow.IsSafeFailure(ex))
        {
            return DirectLoginFlow.Fail(settings, ex);
        }
    }

    static async Task<JsonDocument> ReadIdentity(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var body = new byte[65537];
        var length = 0;
        while (length < body.Length)
        {
            var count = await stream.ReadAsync(body.AsMemory(length), cancellationToken);
            if (count == 0)
            {
                break;
            }

            length += count;
        }

        if (length > 65536)
        {
            throw new DirectAuthError("Direct identity response is too large.");
        }

        return JsonDocument.Parse(body.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 16 });
    }

    static void Render(DirectStatus item)
    {
        AnsiConsole.MarkupLine($"[bold]User:[/]    {item.User.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"[bold]Tenant:[/]  {item.Tenant.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"[bold]Scopes:[/]  {item.Scopes.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"[bold]Expires:[/] {item.ExpiresAt:O}");
        AnsiConsole.MarkupLine("[bold]Stored credentials:[/]");
        foreach (var credential in item.Credentials)
        {
            var tenant = credential.Tenant is null ? "(no tenant)" : $"tenant '{credential.Tenant}'";
            AnsiConsole.MarkupLine($"  {(credential.Active ? "*" : " ")} {credential.Origin.EscapeMarkup()}, {tenant.EscapeMarkup()} ({credential.Store} store)");
        }
    }

    sealed record DirectStatus(string User, string Tenant, string Scopes, DateTimeOffset ExpiresAt, IReadOnlyList<DirectStoredCredential> Credentials);
}

/// <summary>Non-secret metadata about a stored Direct credential, as shown by status.</summary>
/// <param name="Origin">Direct origin.</param>
/// <param name="Tenant">Tenant hint, or null.</param>
/// <param name="Store">"os" for the OS credential manager, "file" for the plaintext file store.</param>
/// <param name="Active">Whether this is the active origin and tenant.</param>
internal sealed record DirectStoredCredential(string Origin, string? Tenant, string Store, bool Active);
