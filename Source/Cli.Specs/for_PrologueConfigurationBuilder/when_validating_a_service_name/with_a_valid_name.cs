// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_validating_a_service_name;

public class with_a_valid_name : Specification
{
    bool _result;

    void Because() => _result = PrologueConfigurationBuilder.IsValidServiceName("orders-api", ["orders"]);

    [Fact] void should_accept_it() => _result.ShouldBeTrue();
}
