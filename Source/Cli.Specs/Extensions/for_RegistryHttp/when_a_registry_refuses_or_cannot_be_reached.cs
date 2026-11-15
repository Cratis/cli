// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Extensions.for_RegistryHttp;

public class when_a_registry_refuses_or_cannot_be_reached : Specification
{
    const string Password = "s3cr3t-pa55word";
    const string Host = "registry.internal";

    ExtensionFailure _noCredential;
    ExtensionFailure _wrongCredential;
    ExtensionFailure _unreachable;

    static HttpResponseMessage Challenge()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ParseAdd("Basic realm=\"registry\"");
        return response;
    }

    static OciExtensionRegistry Registry(FakeRegistryServer server, RegistryCredential? credential) =>
        new(new RegistryHttp(new HttpClient(server), new FixedCredentials(credential)));

    async Task Because()
    {
        var refusing = new FakeRegistryServer { Intercept = _ => Challenge() };
        _noCredential = (ExtensionFailure)await Catch.Exception(() => Registry(refusing, null).Versions($"{Host}/{OciArtifact.Repository}", CancellationToken.None));
        _wrongCredential = (ExtensionFailure)await Catch.Exception(() => Registry(refusing, new("ci", Password, null)).Versions($"{Host}/{OciArtifact.Repository}", CancellationToken.None));
        var calls = 0;
        var broken = new FakeRegistryServer { Intercept = _ => ++calls == 1 ? Challenge() : throw new HttpRequestException($"connection to https://ci:{Password}@{Host} failed") };
        _unreachable = (ExtensionFailure)await Catch.Exception(() => Registry(broken, new("ci", Password, null)).Versions($"{Host}/{OciArtifact.Repository}", CancellationToken.None));
    }

    [Fact] void should_fail_as_unauthorized_without_a_credential() => _noCredential.Kind.ShouldEqual(ExtensionFailureKind.Unauthorized);
    [Fact] void should_say_where_to_store_a_credential() => _noCredential.Message.ShouldContain("cratis config set extensions.registry.registry.internal.token --secret");
    [Fact] void should_fail_as_unauthorized_with_a_refused_credential() => _wrongCredential.Kind.ShouldEqual(ExtensionFailureKind.Unauthorized);
    [Fact] void should_never_print_the_credential_when_it_is_refused() => _wrongCredential.Message.ShouldNotContain(Password);
    [Fact] void should_fail_as_unreachable() => _unreachable.Kind.ShouldEqual(ExtensionFailureKind.Unreachable);
    [Fact] void should_never_print_the_credential_when_the_registry_cannot_be_reached() => _unreachable.Message.ShouldNotContain(Password);
}
