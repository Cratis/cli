// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Sends requests to one registry host, answering its authentication challenge from the host's credential.
/// </summary>
/// <remarks>
/// A request goes out anonymously first; the credential is read (and a passphrase asked for) only when the registry challenges.
/// </remarks>
/// <param name="http">The <see cref="HttpClient"/>.</param>
/// <param name="credentials">Where credentials come from.</param>
public partial class RegistryHttp(HttpClient http, IRegistryCredentials credentials)
{
    static readonly string[] _challengeFields = ["service", "scope"];

    readonly Dictionary<string, string> _tokens = [];
    readonly Dictionary<string, RegistryCredential?> _known = [];

    [GeneratedRegex("(?<key>\\w+)=\"(?<value>[^\"]*)\"", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ChallengeParameter { get; }

    /// <summary>
    /// Sends a request; a challenge is answered once.
    /// </summary>
    /// <param name="host">The registry host, with its port when it is not the default.</param>
    /// <param name="reference">The reference as the model writes it, for messages.</param>
    /// <param name="request">Creates the request; called again after a challenge.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The response; the caller disposes it.</returns>
    /// <exception cref="ExtensionFailure">Thrown when the registry cannot be reached or refuses the credentials.</exception>
    public async Task<HttpResponseMessage> Send(string host, string reference, Func<HttpRequestMessage> request, CancellationToken cancellationToken)
    {
        _known.TryGetValue(host, out var credential);
        try
        {
            var response = await Dispatch(host, request, credential, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized) return Check(response, host, reference, credential);
            var challenge = response.Headers.WwwAuthenticate.FirstOrDefault();
            response.Dispose();
            if (!_known.ContainsKey(host)) _known[host] = credential = await credentials.For(host, cancellationToken);
            if (challenge is null) throw Refused(host, reference, credential);
            await Answer(host, reference, challenge, credential, cancellationToken);
            return Check(await Dispatch(host, request, credential, cancellationToken), host, reference, credential);
        }
        catch (HttpRequestException exception)
        {
            throw new ExtensionFailure(ExtensionFailureKind.Unreachable, reference, $"Could not reach {host}: {Redact(exception.Message, credential)}");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExtensionFailure(ExtensionFailureKind.Unreachable, reference, $"Timed out waiting for {host}: {Redact(exception.Message, credential)}");
        }
    }

    static string Redact(string message, RegistryCredential? credential) => credential?.Redact(message) ?? message;

    static ExtensionFailure Refused(string host, string reference, RegistryCredential? credential)
    {
        var key = ConfiguredRegistryCredentials.KeyOf(host, "token");
        var message = credential is null
            ? $"{host} requires authentication and no credential is configured. Store one with 'cratis config set {key} --secret'."
            : $"{host} refused the configured credential. Check {key}, or its username and password.";
        return new(ExtensionFailureKind.Unauthorized, reference, message);
    }

    static AuthenticationHeaderValue? Basic(RegistryCredential? credential)
    {
        if (credential is null || credential.Secrets.FirstOrDefault() is not { } secret) return null;
        return new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credential.Username ?? "token"}:{credential.Password ?? secret}")));
    }

    static string TokenOf(JsonElement response)
    {
        if (response.TryGetProperty("token", out var token)) return token.GetString() ?? string.Empty;
        return response.TryGetProperty("access_token", out var access) ? access.GetString() ?? string.Empty : string.Empty;
    }

    HttpResponseMessage Check(HttpResponseMessage response, string host, string reference, RegistryCredential? credential)
    {
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)) return response;
        response.Dispose();
        throw Refused(host, reference, credential);
    }

    async Task Answer(string host, string reference, AuthenticationHeaderValue challenge, RegistryCredential? credential, CancellationToken cancellationToken)
    {
        if (!challenge.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            if (credential is null) throw Refused(host, reference, credential);
            return;
        }

        var token = await Token(challenge.Parameter ?? string.Empty, credential, cancellationToken);
        if (token.Length == 0) throw Refused(host, reference, credential);
        _tokens[host] = token;
    }

    async Task<HttpResponseMessage> Dispatch(string host, Func<HttpRequestMessage> request, RegistryCredential? credential, CancellationToken cancellationToken)
    {
        var message = request();
        if (_tokens.TryGetValue(host, out var bearer)) message.Headers.Authorization = new("Bearer", bearer);
        else if (credential is { Password: not null } or { Token: not null }) message.Headers.Authorization = Basic(credential);
        return await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    async Task<string> Token(string parameters, RegistryCredential? credential, CancellationToken cancellationToken)
    {
        var values = ChallengeParameter.Matches(parameters).ToDictionary(match => match.Groups["key"].Value, match => match.Groups["value"].Value);
        if (!values.TryGetValue("realm", out var realm)) return string.Empty;
        var query = string.Join('&', _challengeFields.Where(values.ContainsKey).Select(key => $"{key}={Uri.EscapeDataString(values[key])}"));
        using var request = new HttpRequestMessage(HttpMethod.Get, query.Length == 0 ? realm : $"{realm}?{query}");
        request.Headers.Authorization = Basic(credential);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return string.Empty;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return TokenOf(document.RootElement);
    }
}
