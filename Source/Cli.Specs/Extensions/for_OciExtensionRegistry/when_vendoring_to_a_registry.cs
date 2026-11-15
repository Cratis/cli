// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Extensions.for_OciExtensionRegistry;

public class when_vendoring_to_a_registry : Specification
{
    const string Source = "ghcr.io/" + OciArtifact.Repository;

    FakeRegistryServer _origin;
    FakeRegistryServer _mirror;
    RegistryPackage _package;
    RegistryPackage _roundTrip;
    Dictionary<string, byte[]> _manifests;
    HashSet<string> _blobs;
    Exception _npm;

    void Establish()
    {
        _manifests = [];
        _blobs = [];
        _origin = new();
        OciArtifact.Publish(_origin, "2.3.1", ExtensionContracts.Stripe2);
        _mirror = new()
        {
            Intercept = request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                if (request.Method == HttpMethod.Post && path.EndsWith("/blobs/uploads/", StringComparison.Ordinal))
                {
                    var accepted = new HttpResponseMessage(HttpStatusCode.Accepted);
                    accepted.Headers.Location = new(path + "session-1", UriKind.Relative);
                    return accepted;
                }

                if (request.Method == HttpMethod.Put && path.Contains("/blobs/uploads/", StringComparison.Ordinal))
                {
                    _blobs.Add(System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query)["digest"]!);
                    return new(HttpStatusCode.Created);
                }

                if (request.Method != HttpMethod.Put) return null;
                _manifests[path] = request.Content!.ReadAsByteArrayAsync().Result;
                return new(HttpStatusCode.Created);
            }
        };
    }

    async Task Because()
    {
        var source = new OciExtensionRegistry(new RegistryHttp(new HttpClient(_origin), new FixedCredentials()));
        _package = (await source.Pull(Source, "2.3.1", CancellationToken.None))!;
        var destination = new OciExtensionRegistry(new RegistryHttp(new HttpClient(_mirror), new FixedCredentials()), "mirror", "registry.internal");
        await destination.Push(_package, CancellationToken.None);
        _npm = await Catch.Exception(() => destination.Push(_package with { Kind = ExtensionPackageKind.Npm }, CancellationToken.None));
        _roundTrip = _package;
    }

    [Fact] void should_push_the_manifest_byte_for_byte_so_the_digest_is_the_same() => FakeRegistryServer.DigestOf(_manifests["/v2/mirror/cratis/extension-stripe/manifests/2.3.1"]).ShouldEqual(_roundTrip.Digest);
    [Fact] void should_push_the_signature_under_the_cosign_tag() => _manifests.ContainsKey($"/v2/mirror/cratis/extension-stripe/manifests/{_roundTrip.Digest.Replace(':', '-')}.sig").ShouldBeTrue();
    [Fact] void should_upload_every_blob_of_the_artifact() => _roundTrip.Parts.Where(part => part.Name != "manifest.json").All(part => _blobs.Contains(FakeRegistryServer.DigestOf(part.Content))).ShouldBeTrue();
    [Fact] void should_upload_the_signature_payload() => _blobs.Contains(FakeRegistryServer.DigestOf(_roundTrip.Signature!.Payload)).ShouldBeTrue();
    [Fact] void should_describe_the_destination() => new OciExtensionRegistry(new RegistryHttp(new HttpClient(_mirror), new FixedCredentials()), "mirror", "registry.internal").Description.ShouldEqual("oci://registry.internal/mirror");
    [Fact] void should_refuse_an_npm_package() => _npm.ShouldBeOfExactType<ExtensionFailure>();
}
