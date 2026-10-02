// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Registration;

namespace Cratis.Cli.for_DiagnoseCommand;

public class when_listing_usage_examples : Specification
{
    string[] _examples;

    void Because() => _examples = [.. typeof(DiagnoseCommand).GetCustomAttributes(typeof(CliExampleAttribute), false).Cast<CliExampleAttribute>().Select(example => string.Join(' ', example.Args))];

    [Fact] void should_demonstrate_all_namespace_json_output() => _examples.ShouldContain("chronicle diagnose --all-namespaces -o json");
    [Fact] void should_demonstrate_all_event_store_json_output() => _examples.ShouldContain("chronicle diagnose --all-event-stores -o json");
}
