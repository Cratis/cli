// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitValidation;

public class when_loading_a_folder_without_a_manifest : given.a_kit_folder
{
    Exception _error;

    void Establish() => File.Delete(Path.Combine(_kit, BuildKitManifestText.YamlFileName));

    void Because() => _error = Catch.Exception(() => BuildKit.Load(_kit));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<InvalidBuildKit>();
    [Fact] void should_say_the_manifest_is_missing() => ((InvalidBuildKit)_error).Problems.Single().ShouldContain(BuildKitManifestText.YamlFileName);
}
