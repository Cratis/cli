// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_parsing_a_comparison;

public class and_the_status_is_missing : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.ParseComparison("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", """{"message":"Server Error"}""");

    [Fact] void should_not_read_a_comparison() => _result.ShouldBeNull();
}
