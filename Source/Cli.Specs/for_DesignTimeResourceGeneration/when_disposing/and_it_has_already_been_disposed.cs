// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration.when_disposing;

public class and_it_has_already_been_disposed : Specification
{
    DesignTimeResourceGeneration _generation;
    Exception _error;

    void Establish()
    {
        _generation = DesignTimeResourceGeneration.Create();
        _generation.Dispose();
    }

    void Because() => _error = Catch.Exception(_generation.Dispose);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
