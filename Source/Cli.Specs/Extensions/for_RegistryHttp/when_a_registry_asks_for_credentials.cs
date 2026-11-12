// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Cli.Extensions.for_RegistryHttp;

public class when_a_registry_asks_for_credentials : Specification
{
    const string Password = "s3cr3t-pa55word";
    const string Host = "registry.internal";

    FakeRegistryServer _server;
    FixedCredentials _credentials;
    OciExtensionRegistry _registry;
    IReadOnlyList<string> _versions;
    string _tokenRequestAuthorization;

    void Establish()
    {
        _server = new();
        OciArtifact.ListTags(_server, "2.3.1");
        _server.Serve("/token?service=registry.internal&scope=repository%3Acratis%2Fextension-stripe%3Apull", "{\"token\":\"issued-token\"}");
        _server.Intercept = request =>
        {
            if (request.RequestUri!.AbsolutePath == "/token")
            {
                _tokenRequestAuthorization = request.Headers.Authorization?.Parameter;
                return null;
            }

            if (request.Headers.Authorization?.Parameter == "issued-token") return null;
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.ParseAdd("Bearer realm=\"https://registry.internal/token\",service=\"registry.internal\",scope=\"repository:cratis/extension-stripe:pull\"");
            return response;
        };
        _credentials = new(new("ci", Password, null));
        _registry = new(new RegistryHttp(new HttpClient(_server), _credentials));
    }

    async Task Because() => _versions = await _registry.Versions($"{Host}/{OciArtifact.Repository}", CancellationToken.None);

    [Fact] void should_list_the_versions() => _versions.Single().ShouldEqual("2.3.1");
    [Fact] void should_ask_the_registry_for_a_token_with_the_configured_credential() => _tokenRequestAuthorization.ShouldEqual(Convert.ToBase64String(Encoding.UTF8.GetBytes($"ci:{Password}")));
    [Fact] void should_use_the_issued_token_afterwards() => _server.Authorizations[^1].ShouldEqual("Bearer issued-token");
    [Fact] void should_read_the_credential_once() => _credentials.Asked.ShouldEqual(1);
    [Fact] void should_ask_anonymously_first() => _server.Authorizations[0].ShouldBeNull();
}
