// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_normalizing_base_path;

public class with_empty_value : Specification
{
    string _result;

    void Because() => _result = PrologueConfigurationBuilder.NormalizeBasePath("  ");

    [Fact] void should_fall_back_to_the_default_base_path() => _result.ShouldEqual(PrologueConfigurationBuilder.DefaultApiBasePath);
}
