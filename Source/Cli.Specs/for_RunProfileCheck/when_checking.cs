// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RunProfileCheck;

public class when_checking : Specification
{
    const string UnknownPackageCode = "CLI-RENDER-007";

    const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [] } }" +
        "] }";

    string _root;
    IReadOnlyList<ScreenplayDiagnostic> _unapproved;
    IReadOnlyList<ScreenplayDiagnostic> _approved;
    IReadOnlyList<ScreenplayDiagnostic> _shipped;
    IReadOnlyList<ScreenplayDiagnostic> _notCompiling;
    IReadOnlyList<ScreenplayDiagnostic> _singleFile;

    void Establish() => _root = Directory.CreateTempSubdirectory("cratis-run-profile-").FullName;

    void Because()
    {
        var withPackage = Directory.CreateDirectory(Path.Combine(_root, "custom")).FullName;
        File.WriteAllText(Path.Combine(withPackage, "application.play"), Application("    Acme.Charts\n"));

        var shippedOnly = Directory.CreateDirectory(Path.Combine(_root, "shipped")).FullName;
        File.WriteAllText(Path.Combine(shippedOnly, "application.play"), Application(string.Empty));

        var broken = Directory.CreateDirectory(Path.Combine(_root, "broken")).FullName;
        File.WriteAllText(Path.Combine(broken, "application.play"), "this is not screenplay\n");

        var approved = ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames).Set!;
        _unapproved = RunProfileCheck.Check(new DirectoryInfo(withPackage), ApprovedPackageSet.Empty);
        _approved = RunProfileCheck.Check(new DirectoryInfo(withPackage), approved);
        _shipped = RunProfileCheck.Check(new DirectoryInfo(shippedOnly), ApprovedPackageSet.Empty);
        _notCompiling = RunProfileCheck.Check(new DirectoryInfo(broken), ApprovedPackageSet.Empty);
        _singleFile = RunProfileCheck.Check(new FileInfo(Path.Combine(withPackage, "application.play")), ApprovedPackageSet.Empty);
    }

    void Destroy() => Directory.Delete(_root, true);

    static string Application(string extraPackages) =>
        "layout AppShell\n  topbar\n  content\n\n  arrangement flow\n    column\n      topbar height 56\n      content grow\n\n" +
        "ui profile Desktop\n  target platform web\n  target size expanded\n  layout AppShell\n  packages\n    core\n    Cratis.Components\n" + extraPackages;

    [Fact] void should_refuse_a_package_that_is_neither_shipped_nor_approved() => _unapproved.Single().Code.ShouldEqual(UnknownPackageCode);
    [Fact] void should_name_the_package() => _unapproved.Single().Message.ShouldContain("Acme.Charts");
    [Fact] void should_accept_the_package_once_it_is_approved() => _approved.ShouldBeEmpty();
    [Fact] void should_accept_the_shipped_packages() => _shipped.ShouldBeEmpty();
    [Fact] void should_leave_a_model_that_does_not_compile_to_the_stage() => _notCompiling.ShouldBeEmpty();
    [Fact] void should_check_a_single_file_the_same_way() => _singleFile.Count.ShouldEqual(1);
}
