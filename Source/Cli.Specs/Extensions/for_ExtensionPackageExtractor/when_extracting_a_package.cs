// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionPackageExtractor;

public class when_extracting_a_package : Specification
{
    string _oci = null!;
    string _npm = null!;
    string _titled = null!;
    Exception? _traversal;

    static string NewFolder() => Path.Combine(Path.GetTempPath(), "cratis-specs-extension-extract", Guid.NewGuid().ToString("N"));

    static RegistryPackage Package(ExtensionPackageKind kind, params ArtifactPart[] parts) => new(kind, "ref", "1.0.0", "sha256:aa", [], parts, null);

    void Because()
    {
        _oci = NewFolder();
        ExtensionPackageExtractor.Extract(
            Package(
                ExtensionPackageKind.Oci,
                new ArtifactPart("layer", "application/vnd.oci.image.layer.v1.tar+gzip", TarLayer.Build(true, new Dictionary<string, string> { ["./extension/contract.play"] = "c", ["extension/assets/icon.svg"] = "i", ["etc/passwd"] = "x" }))),
            _oci);

        _titled = NewFolder();
        ExtensionPackageExtractor.Extract(Package(ExtensionPackageKind.Oci, new ArtifactPart("contract.play", "text/plain", [99])), _titled);

        _npm = NewFolder();
        ExtensionPackageExtractor.Extract(
            Package(ExtensionPackageKind.Npm, new ArtifactPart(NpmExtensionRegistry.TarballPart, "application/gzip", TarLayer.Build(true, new Dictionary<string, string> { ["package/contract.play"] = "c", ["package/frontend/index.js"] = "j", ["other/x"] = "x" }))),
            _npm);

        _traversal = Catch.Exception(() => ExtensionPackageExtractor.Extract(
            Package(ExtensionPackageKind.Oci, new ArtifactPart("layer", "application/x-tar", TarLayer.Build(false, new Dictionary<string, string> { ["extension/../../escape.txt"] = "x" }))),
            NewFolder()));
    }

    [Fact] void should_lay_an_oci_artifact_out_from_its_extension_folder() => Directory.GetFiles(_oci, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(_oci, file).Replace('\\', '/')).Order().ShouldEqual(["assets/icon.svg", "contract.play"]);
    [Fact] void should_take_a_contract_layer_as_the_contract() => File.ReadAllText(Path.Combine(_titled, "contract.play")).ShouldEqual("c");
    [Fact] void should_lay_an_npm_package_out_from_its_package_folder() => Directory.GetFiles(_npm, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(_npm, file).Replace('\\', '/')).Order().ShouldEqual(["contract.play", "frontend/index.js"]);
    [Fact] void should_refuse_an_entry_outside_the_package() => _traversal.ShouldBeOfExactType<InvalidDataException>();
}
