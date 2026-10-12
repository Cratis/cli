// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_StagePackageMounts;

public class when_resolving : Specification
{
    const string Document =
        "{ \"version\": 1, \"packages\": [" +
        "{ \"manifest\": { \"name\": \"Acme.Charts\", \"version\": \"1.2.0\", \"kind\": \"ComponentLibrary\", \"components\": [\"chart\"] }," +
        " \"module\": { \"npm\": \"@acme/scene.charts\", \"version\": \"1.2.0\", \"export\": \"acmeChartsRuntimePackage\", \"styles\": [] } }" +
        "] }";

    string _root;
    ApprovedPackageSet _set;
    (StagePackageMounts? Mounts, string? Error) _defaultFolder;
    (StagePackageMounts? Mounts, string? Error) _otherFolder;
    (StagePackageMounts? Mounts, string? Error) _missing;
    (StagePackageMounts? Mounts, string? Error) _noFolder;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("cratis-package-mounts-").FullName;
        _set = ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames).Set!;
        Directory.CreateDirectory(Path.Combine(_root, "@acme", "scene.charts"));
        Directory.CreateDirectory(Path.Combine(_root, "modules", "@acme", "scene.charts"));
    }

    void Because()
    {
        var setFile = Path.Combine(_root, "approved-packages.json");
        _defaultFolder = StagePackageMounts.Resolve(setFile, _set, null);
        _otherFolder = StagePackageMounts.Resolve(setFile, _set, Path.Combine(_root, "modules"));
        _missing = StagePackageMounts.Resolve(setFile, _set, Path.Combine(_root, "modules", "@acme"));
        _noFolder = StagePackageMounts.Resolve(setFile, _set, Path.Combine(_root, "nowhere"));
    }

    void Destroy() => Directory.Delete(_root, true);

    [Fact] void should_find_the_package_in_the_folder_the_set_is_in() => _defaultFolder.Mounts!.Packages.Single().Folder.ShouldEqual(Path.Combine(_root, "@acme", "scene.charts"));
    [Fact] void should_find_the_package_in_the_folder_it_was_asked_for() => _otherFolder.Mounts!.Packages.Single().Folder.ShouldEqual(Path.Combine(_root, "modules", "@acme", "scene.charts"));
    [Fact] void should_name_the_package_by_its_npm_name() => _defaultFolder.Mounts!.Packages.Single().Npm.ShouldEqual("@acme/scene.charts");
    [Fact] void should_refuse_a_package_without_a_folder() => _missing.Error!.ShouldContain("Acme.Charts");
    [Fact] void should_refuse_a_folder_that_does_not_exist() => _noFolder.Error!.ShouldContain("does not exist");
}
