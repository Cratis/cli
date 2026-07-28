// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration.when_disposing;

public class and_the_targets_file_was_written : Specification
{
    DesignTimeResourceGeneration _generation;
    string _file;

    void Establish()
    {
        _generation = DesignTimeResourceGeneration.Create();
        _file = _generation.GlobalProperties[DesignTimeResourceGeneration.HookProperty];
    }

    void Because() => _generation.Dispose();

    [Fact] void should_remove_the_targets_file() => File.Exists(_file).ShouldBeFalse();
}
