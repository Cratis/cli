// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_validating_a_service_name;

public class with_the_default_cluster_name : Specification
{
    bool _result;

    void Because() => _result = PrologueConfigurationBuilder.IsValidServiceName("monitored", ["orders"]);

    [Fact] void should_reject_it() => _result.ShouldBeFalse();
}
