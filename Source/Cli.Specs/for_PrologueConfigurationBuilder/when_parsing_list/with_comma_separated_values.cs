// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_parsing_list;

public class with_comma_separated_values : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = PrologueConfigurationBuilder.ParseList(" Authors, Books , ,Loans ");

    [Fact] void should_trim_and_drop_empty_entries() => _result.ShouldEqual("Authors", "Books", "Loans");
}
