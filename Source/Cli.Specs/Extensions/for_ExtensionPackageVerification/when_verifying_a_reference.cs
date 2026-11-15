// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionPackageVerification;

public class when_verifying_a_reference : given.a_valid_package
{
    ExtensionVerifyReport _fetched = null!;
    ExtensionVerifyReport _locked = null!;
    ExtensionVerifyReport _drifted = null!;
    string? _range;
    int _leftBehind = -1;

    static string Scratch => Path.Combine(Path.GetTempPath(), "cratis-extension-verify");

    static ExtensionLockEntry Entry(string digest, string hash) => new("cratis/demo", "^1", "ghcr.io/cratis/demo", "1.2.0", digest, hash, "cratis");

    RegistryPackage Package()
    {
        var layer = TarLayer.Build(true, Directory.GetFiles(Folder, "*", SearchOption.AllDirectories).ToDictionary(file => "extension/" + Path.GetRelativePath(Folder, file).Replace('\\', '/'), File.ReadAllText));
        return new(ExtensionPackageKind.Oci, "ghcr.io/cratis/demo", "1.2.0", "sha256:aa", File.ReadAllBytes(Path.Combine(Folder, "contract.play")), [new("layer", "application/vnd.oci.image.layer.v1.tar+gzip", layer)], null);
    }

    async Task Because()
    {
        // The binary icon cannot survive a text tar, so the package carries SVG artwork instead.
        Write("assets/icon.png", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        Write("contract.play", Contract.Replace("icon.png", "icon.svg", StringComparison.Ordinal).Replace("shot.png", "shot.svg", StringComparison.Ordinal));
        Write("assets/icon.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        Write("assets/shot.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        File.Delete(Path.Combine(Folder, "assets", "icon.png"));
        File.Delete(Path.Combine(Folder, "assets", "shot.png"));
        var before = Directory.Exists(Scratch) ? Directory.GetDirectories(Scratch).Length : 0;
        var package = Package();
        var verification = new ExtensionPackageVerification(Verifier, (_, range, _, _) =>
        {
            _range = range;
            return Task.FromResult(package);
        });
        var hash = ExtensionHashes.Of(package.Contract);

        _fetched = await verification.VerifyReference("cratis/demo", "^1", null, new(CompilerVersion: "6.22.0"), CancellationToken.None);
        _locked = await verification.VerifyReference("cratis/demo", "^1", Entry("sha256:aa", hash), new(CompilerVersion: "6.22.0"), CancellationToken.None);
        _drifted = await verification.VerifyReference("cratis/demo", "^1", Entry("sha256:bb", hash), new(CompilerVersion: "6.22.0"), CancellationToken.None);
        _leftBehind = (Directory.Exists(Scratch) ? Directory.GetDirectories(Scratch).Length : 0) - before;
    }

    [Fact] void should_verify_what_the_registry_served() => _fetched.Passed.ShouldBeTrue();
    [Fact] void should_name_the_package_and_version() => _fetched.Target.ShouldEqual("ghcr.io/cratis/demo 1.2.0");
    [Fact] void should_pass_the_range_to_the_registry() => _range.ShouldEqual("^1");
    [Fact] void should_check_the_digests_when_the_lock_pins_the_package() => _locked.Checks.Single(check => check.Check == "digests").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_fail_a_package_that_is_not_the_one_the_lock_pins() => _drifted.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ExtensionVerifyCodes.DigestMismatch);
    [Fact] void should_not_leave_the_extracted_package_behind() => _leftBehind.ShouldEqual(0);
}
