// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitValidation;

public class when_loading_a_valid_kit : given.a_kit_folder
{
    BuildKit _loaded;

    void Because() => _loaded = BuildKit.Load(_kit);

    [Fact] void should_read_the_name() => _loaded.Name.ShouldEqual("shell-demo");
    [Fact] void should_read_the_slice_types() => _loaded.SliceTypes.ToArray().ShouldEqual(["stateChange", "stateView"]);
    [Fact] void should_read_ui_as_a_boolean() => _loaded.BuildsUserInterfaces.ShouldBeFalse();
    [Fact] void should_read_the_verification_commands() => _loaded.Verify.Count.ShouldEqual(2);
    [Fact] void should_read_the_permitted_paths() => _loaded.PermittedPaths.ToArray().ShouldEqual(["src/**"]);
    [Fact] void should_read_the_conventions_path() => _loaded.Conventions.ShouldEqual(".cratis/build/conventions.md");
    [Fact] void should_default_the_contract_to_version_one() => _loaded.ContractVersions.ToArray().ShouldEqual(["1"]);
}
