// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitPlaceholders;

public class when_filling : Specification
{
    IReadOnlyDictionary<string, string> _values;
    string _prompt;
    string _command;
    string _withJson;

    void Establish() => _values = BuildKitPlaceholders.For(given.an_order.Parse(), "/tmp/work/order.json", "/tmp/my outcome.json", string.Empty);

    void Because()
    {
        _prompt = BuildKitPlaceholders.Fill("Build {slice} of {featurePath} in {application} ({part}, attempt {attempt}).", _values);
        _command = BuildKitPlaceholders.FillCommand("check {outcome} {slice}", _values);
        _withJson = BuildKitPlaceholders.Fill("Write {{\"kind\": \"built\"}} and { \"a\": 1 } for {slice}.", _values);
    }

    [Fact] void should_fill_every_placeholder() => _prompt.ShouldEqual("Build PayInvoice of Billing/Invoicing in Shop (backend, attempt 2).");
    [Fact] void should_quote_a_value_a_shell_would_split() => _command.ShouldEqual("check '/tmp/my outcome.json' PayInvoice");
    [Fact] void should_leave_json_and_double_braces_alone() => _withJson.ShouldEqual("Write {{\"kind\": \"built\"}} and { \"a\": 1 } for PayInvoice.");
    [Fact] void should_find_a_placeholder_no_host_fills() => BuildKitPlaceholders.Unknown("{slice} {token} {{order}}").ToArray().ShouldEqual(["token"]);
}
