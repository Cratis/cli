// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_parsing_list;

public class with_empty_value : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = PrologueConfigurationBuilder.ParseList(null);

    [Fact] void should_be_empty() => _result.ShouldBeEmpty();
}
