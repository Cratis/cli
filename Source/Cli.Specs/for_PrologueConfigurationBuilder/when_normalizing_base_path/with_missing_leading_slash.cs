// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_normalizing_base_path;

public class with_missing_leading_slash : Specification
{
    string _result;

    void Because() => _result = PrologueConfigurationBuilder.NormalizeBasePath("api");

    [Fact] void should_add_the_leading_slash() => _result.ShouldEqual("/api");
}
