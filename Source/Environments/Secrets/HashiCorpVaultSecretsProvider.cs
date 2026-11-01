// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cratis.Environments.Secrets;

/// <summary>
/// Keeps secrets in a HashiCorp Vault KV v2 mount over its HTTP API; the environment file holds the secret path as a reference.
/// </summary>
public sealed class HashiCorpVaultSecretsProvider : ISecretsProvider, IDisposable
{
    const string ValueField = "value";

    readonly HttpClient _http;
    readonly string _mount;
    readonly string _prefix;
    readonly string _environment;
    string? _token;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashiCorpVaultSecretsProvider"/> class.
    /// </summary>
    /// <param name="spec">The parsed <c language="csharp">hashivault://</c> specification.</param>
    /// <param name="environment">The environment name, used in the secret path.</param>
    /// <param name="handler">Optional HTTP handler, for tests.</param>
    public HashiCorpVaultSecretsProvider(SecretsProviderSpec spec, string environment, HttpMessageHandler? handler = null)
    {
        var parts = spec.Target.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
        var scheme = spec.Text.StartsWith("hashivault+http://", StringComparison.OrdinalIgnoreCase) ? "http" : "https";
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.BaseAddress = new Uri($"{scheme}://{parts[0]}/");
        _mount = parts[1];
        _prefix = parts.Length > 2 ? parts[2].Trim('/') : "cratis";
        _environment = environment;
        var ns = Environment.GetEnvironmentVariable("VAULT_NAMESPACE");
        if (!string.IsNullOrEmpty(ns))
        {
            _http.DefaultRequestHeaders.Add("X-Vault-Namespace", ns);
        }
    }

    /// <summary>
    /// Builds the logical secret path for a key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The path below the mount.</returns>
    public string PathFor(string key) => $"{_prefix}/{_environment}/{key}";

    /// <inheritdoc/>
    public async Task<ConfigEntry> StoreAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var path = PathFor(key);
        using var request = await CreateAsync(HttpMethod.Put, $"v1/{_mount}/data/{Escape(path)}", cancellationToken);
        request.Content = JsonContent.Create(new { data = new Dictionary<string, string> { [ValueField] = value } });
        using var response = await _http.SendAsync(request, cancellationToken);
        Ensure(response, $"store '{key}'");
        return new ConfigEntry(ConfigEntryKind.Reference, path);
    }

    /// <inheritdoc/>
    public async Task<string> ResolveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default)
    {
        using var request = await CreateAsync(HttpMethod.Get, $"v1/{_mount}/data/{Escape(entry.Value)}", cancellationToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new SecretNotAvailableException($"The secret for '{key}' ({entry.Value}) does not exist in Vault.");
        }

        Ensure(response, $"read '{key}'");
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return json.RootElement.GetProperty("data").GetProperty("data").GetProperty(ValueField).GetString()
            ?? throw new SecretNotAvailableException($"The secret for '{key}' has no value in Vault.");
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Kind != ConfigEntryKind.Reference)
        {
            return;
        }

        using var request = await CreateAsync(HttpMethod.Delete, $"v1/{_mount}/metadata/{Escape(entry.Value)}", cancellationToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            Ensure(response, $"remove '{key}'");
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _http.Dispose();

    static string Escape(string path) => string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    static void Ensure(HttpResponseMessage response, string action)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new SecretNotAvailableException($"HashiCorp Vault could not {action}: HTTP {(int)response.StatusCode}.");
        }
    }

    async Task<HttpRequestMessage> CreateAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        _token ??= await VaultAuthentication.GetTokenAsync(_http, cancellationToken);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Vault-Token", _token);
        return request;
    }
}
