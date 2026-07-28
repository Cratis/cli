// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration;

public class when_creating : Specification
{
    DesignTimeResourceGeneration _generation;
    string _file;

    void Because()
    {
        _generation = DesignTimeResourceGeneration.Create();
        _file = _generation.GlobalProperties[DesignTimeResourceGeneration.HookProperty];
    }

    [Fact] void should_hook_the_targets_file_into_every_project() => DesignTimeResourceGeneration.HookProperty.ShouldEqual("CustomAfterMicrosoftCommonTargets");
    [Fact] void should_write_the_targets_file() => File.Exists(_file).ShouldBeTrue();
    [Fact] void should_write_the_resource_target_chain_into_it() => File.ReadAllText(_file).ShouldContain("PrepareResources");

    void Destroy() => _generation.Dispose();
}
