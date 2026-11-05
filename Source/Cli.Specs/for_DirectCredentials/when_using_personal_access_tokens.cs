// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectMcpBridge.given;

namespace Cratis.Cli.for_DirectCredentials;

public class when_using_personal_access_tokens : Specification
{
    const string Token = "dpat_example-for-specifications";
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", null);
    readonly DirectCredentialEntry _entry = new() { Origin = "https://direct.example", Kind = DirectCredentialEntry.PersonalAccessToken };

    [Fact] async Task should_read_from_stdin() => (await DirectPersonalAccessLogin.Read(null, new StringReader(Token), () => null, CancellationToken.None)).ShouldEqual(Token);
    [Fact] async Task should_read_from_environment() => (await DirectPersonalAccessLogin.Read(null, new StringReader(""), () => Token, CancellationToken.None)).ShouldEqual(Token);
    [Fact] async Task should_force_stdin_with_a_dash() => (await DirectPersonalAccessLogin.Read("-", new StringReader(Token), () => "ignored", CancellationToken.None)).ShouldEqual(Token);
    [Fact] void should_reject_an_invalid_prefix() => DirectPersonalAccessLogin.IsValid("not-a-token").ShouldBeFalse();
    [Fact] void should_reject_an_empty_suffix() => DirectPersonalAccessLogin.IsValid("dpat_").ShouldBeFalse();
    [Fact] void should_reject_newlines() => DirectPersonalAccessLogin.IsValid(Token + "\n").ShouldBeFalse();
    [Fact] void should_refuse_a_tenant() => Assert.Throws<DirectAuthError>(() => DirectPersonalAccessLogin.ValidateOptions("team", null)).Message.ShouldContain("--tenant");
    [Fact] void should_refuse_an_issuer() => Assert.Throws<DirectAuthError>(() => DirectPersonalAccessLogin.ValidateOptions(null, "https://identity.example")).Message.ShouldContain("--issuer");

    [Fact]
    async Task should_store_without_an_issuer_or_refresh_token()
    {
        var store = new MemoryStore();
        var config = await Store(store);
        var stored = JsonSerializer.Deserialize<DirectTokens>(store.Value ?? throw new InvalidOperationException("Secret was not stored.")) ?? throw new InvalidOperationException("Secret was invalid.");
        stored.AccessToken.ShouldEqual(Token);
        stored.RefreshToken.ShouldBeNull();
        stored.Issuer.ShouldBeNull();
        var selection = config.Direct ?? throw new InvalidOperationException("Selection was not stored.");
        selection.Issuer.ShouldBeNull();
        selection.HasActiveSelection.ShouldBeTrue();
        JsonSerializer.Serialize(config).Contains(Token).ShouldBeFalse();
    }

    [Fact]
    async Task should_select_the_static_provider_and_return_the_token()
    {
        var store = new MemoryStore();
        await Store(store);
        using var http = new HttpClient(new NoNetwork());
        var provider = DirectLoginFlow.TokenProviderFor(_entry, http, _ => store);
        provider.GetType().ShouldEqual(typeof(DirectStaticTokenProvider));
        (await provider.GetAccessToken(_target, _target.Origin, CancellationToken.None)).ShouldEqual(Token);
    }

    [Fact]
    async Task should_fail_a_401_without_refresh_or_discovery()
    {
        var store = new MemoryStore();
        await Store(store);
        var direct = new a_bridge.Direct();
        direct.Answer(() => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var http = new HttpClient(direct);
        await using var output = new StringWriter();
        await using var log = new StringWriter();
        using var bridge = new DirectMcpBridge(http, new DirectStaticTokenProvider(store), _target, _target.Origin, log);
        await bridge.Run(new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}\n"), output, CancellationToken.None);
        output.ToString().ShouldContain(DirectStaticTokenProvider.Rejected);
        output.ToString().Contains(Token).ShouldBeFalse();
        log.ToString().Contains("refresh").ShouldBeFalse();
        direct.Requests.Count.ShouldEqual(1);
        direct.Requests[0].Authorization.ShouldEqual($"Bearer {Token}");
    }

    [Fact]
    async Task should_show_kind_without_exposing_the_token_in_status()
    {
        var config = await Store(new MemoryStore());
        var status = JsonSerializer.Serialize(DirectStatusCommand.StoredCredentials(config.Direct ?? throw new InvalidOperationException("Selection was not stored."), _target));
        status.ShouldContain("personal-access-token");
        status.Contains(Token).ShouldBeFalse();
    }

    [Fact]
    async Task should_clear_the_active_selection_on_forget()
    {
        var config = await Store(new MemoryStore());
        var selection = config.Direct ?? throw new InvalidOperationException("Selection was not stored.");
        DirectCredentials.Forget(selection, _entry);
        selection.HasActiveSelection.ShouldBeFalse();
        selection.Credentials.Count.ShouldEqual(0);
        DirectLogoutCommand.PersonalAccessTokenRemoved("example").ShouldContain("not revoked server-side");
    }

    [Fact]
    async Task should_roll_back_secret_and_index_if_saving_configuration_fails()
    {
        var config = new CliConfiguration();
        var store = new MemoryStore();
        using var http = new HttpClient(new NoNetwork());
        await Assert.ThrowsAsync<IOException>(() => DirectPersonalAccessLogin.Store(config, _entry, Token, http, () => throw new IOException("simulated failure"), CancellationToken.None, _ => store));
        store.Value.ShouldBeNull();
        config.Direct.ShouldBeNull();
    }

    async Task<CliConfiguration> Store(MemoryStore store)
    {
        var config = new CliConfiguration();
        using var http = new HttpClient(new NoNetwork());
        await DirectPersonalAccessLogin.Store(config, _entry, Token, http, () => { }, CancellationToken.None, _ => store);
        return config;
    }

    sealed class NoNetwork : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new InvalidOperationException("PAT must not discover or refresh.");
    }

    sealed class MemoryStore : IDirectSecretStore
    {
        public string? Value { get; private set; }
        public Task<string?> Read(string key, CancellationToken cancellationToken) => Task.FromResult(Value);
        public Task Write(string key, string value, CancellationToken cancellationToken)
        {
            Value = value;
            return Task.CompletedTask;
        }

        public Task Delete(string key, CancellationToken cancellationToken)
        {
            Value = null;
            return Task.CompletedTask;
        }
    }
}
