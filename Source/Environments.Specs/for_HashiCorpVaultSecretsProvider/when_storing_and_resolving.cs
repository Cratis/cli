// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Environments.for_HashiCorpVaultSecretsProvider;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_storing_and_resolving : Specification
{
    string _previousToken;
    FakeVault _vault;
    ConfigEntry _entry;
    string _resolved;

    async Task Because()
    {
        _previousToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        Environment.SetEnvironmentVariable("VAULT_TOKEN", "test-token");
        _vault = new FakeVault();
        using var provider = new HashiCorpVaultSecretsProvider(SecretsProviderSpec.Parse("hashivault+http://vault:8200/secret/cratis"), "production", _vault);
        _entry = await provider.StoreAsync("smtp:password", "value");
        _resolved = await provider.ResolveAsync("smtp:password", _entry);
        await provider.RemoveAsync("smtp:password", _entry);
    }

    void Destroy() => Environment.SetEnvironmentVariable("VAULT_TOKEN", _previousToken);

    [Fact] void should_use_the_kv_v2_data_path() => _vault.Requests.ShouldContain("PUT http://vault:8200/v1/secret/data/cratis/production/smtp%3Apassword");
    [Fact] void should_send_the_token() => _vault.Tokens.TrueForAll(token => token == "test-token").ShouldBeTrue();
    [Fact] void should_hand_back_the_logical_path_as_reference() => _entry.ShouldEqual(new ConfigEntry(ConfigEntryKind.Reference, "cratis/production/smtp:password"));
    [Fact] void should_resolve_the_value() => _resolved.ShouldEqual("value");
    [Fact] void should_delete_all_versions_on_removal() => _vault.Requests.ShouldContain("DELETE http://vault:8200/v1/secret/metadata/cratis/production/smtp%3Apassword");

    sealed class FakeVault : HttpMessageHandler
    {
        readonly Dictionary<string, string> _store = [];

        public List<string> Requests { get; } = [];

        public List<string> Tokens { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.AbsoluteUri}");
            Tokens.Add(request.Headers.GetValues("X-Vault-Token").Single());
            var path = request.RequestUri.AbsolutePath;
            if (request.Method == HttpMethod.Put)
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                _store[path] = System.Text.Json.JsonDocument.Parse(body).RootElement.GetProperty("data").GetProperty("value").GetString()!;
                return new(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }

            if (request.Method == HttpMethod.Get)
            {
                var json = $"{{\"data\":{{\"data\":{{\"value\":{System.Text.Json.JsonSerializer.Serialize(_store[path])}}}}}}}";
                return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            return new(HttpStatusCode.NoContent);
        }
    }
}
