// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitValidation;

public class when_loading_an_installed_kit_whose_scaffold_was_removed : given.a_kit_folder
{
    BuildKit _installed;
    Exception _asSource;

    void Establish() => Write("build-kit.yaml", $"{Manifest}\nscaffold:\n  path: ./scaffold\n  commands: [\"true\"]");

    void Because()
    {
        _installed = BuildKit.Load(_kit, installed: true);
        _asSource = Catch.Exception(() => BuildKit.Load(_kit));
    }

    [Fact] void should_load_the_installed_copy() => _installed.Name.ShouldEqual("shell-demo");
    [Fact] void should_keep_the_scaffold_path_of_the_manifest() => _installed.ScaffoldPath.ShouldEqual("./scaffold");
    [Fact] void should_still_refuse_a_kit_source_without_its_scaffold() => _asSource.ShouldBeOfExactType<InvalidBuildKit>();
}
