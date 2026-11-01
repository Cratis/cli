// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_validating_a_service_name;

public class with_a_name_already_taken : Specification
{
    bool _result;

    void Because() => _result = PrologueConfigurationBuilder.IsValidServiceName("Orders", ["orders"]);

    [Fact] void should_reject_it() => _result.ShouldBeFalse();
}
