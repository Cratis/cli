// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_OciExtensionRegistry;

public class when_pulling_an_artifact : Specification
{
    FakeRegistryServer _server;
    FixedCredentials _credentials;
    OciExtensionRegistry _registry;
    IReadOnlyList<string> _versions;
    RegistryPackage _package;
    RegistryPackage _unsigned;
    RegistryPackage _missing;
    string _digest;

    void Establish()
    {
        _server = new();
        _credentials = new();
        _digest = OciArtifact.Publish(_server, "2.3.1", ExtensionContracts.Stripe2);
        OciArtifact.Publish(_server, "2.4.0", ExtensionContracts.Stripe3WithRefund, null);
        OciArtifact.ListTags(_server, "2.3.1", "2.4.0", "latest", "sha256-abc.sig", "v3");
        _registry = new(new RegistryHttp(new HttpClient(_server), _credentials));
    }

    async Task Because()
    {
        _versions = await _registry.Versions("ghcr.io/" + OciArtifact.Repository, CancellationToken.None);
        _package = (await _registry.Pull("ghcr.io/" + OciArtifact.Repository, "2.3.1", CancellationToken.None))!;
        _unsigned = (await _registry.Pull("ghcr.io/" + OciArtifact.Repository, "2.4.0", CancellationToken.None))!;
        _missing = await _registry.Pull("ghcr.io/" + OciArtifact.Repository, "9.9.9", CancellationToken.None);
    }

    [Fact] void should_list_only_release_tags() => _versions.Order().ShouldEqual(["2.3.1", "2.4.0"]);
    [Fact] void should_use_the_manifest_digest_as_the_package_digest() => _package.Digest.ShouldEqual(_digest);
    [Fact] void should_read_the_contract_from_the_artifact() => System.Text.Encoding.UTF8.GetString(_package.Contract).ShouldEqual(ExtensionContracts.Stripe2);
    [Fact] void should_keep_the_manifest_and_every_blob() => _package.Parts.Count.ShouldEqual(3);
    [Fact] void should_read_the_signature() => _package.Signature!.Publisher.ShouldEqual("https://github.com/cratis");
    [Fact] void should_verify_the_signature_over_the_digest() => (_package.Signature!.Verifies() && _package.Signature.BindsTo(_package)).ShouldBeTrue();
    [Fact] void should_not_invent_a_signature_for_an_unsigned_package() => _unsigned.Signature.ShouldBeNull();
    [Fact] void should_return_nothing_for_a_release_that_does_not_exist() => _missing.ShouldBeNull();
    [Fact] void should_not_ask_for_a_credential_from_a_registry_that_does_not_ask() => _credentials.Asked.ShouldEqual(0);
}
