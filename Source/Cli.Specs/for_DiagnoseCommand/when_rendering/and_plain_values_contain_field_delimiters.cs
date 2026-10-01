// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_plain_values_contain_field_delimiters : given.captured_reports
{
    void Establish() => _data = _data with
    {
        ChecksCouldNotRun = [new DiagnoseCheckFailure("Observers", "a namespace=b", "tenant=one", "Permission \"denied\" at C:\\store")],
        Findings = [new DiagnoseFinding("Failed partition", "a namespace=b", "tenant=one", "observer\tpartition")],
        Scopes = [_data with { EventStore = "a namespace=b", Namespace = "tenant=one" }]
    };

    void Because() => CaptureReports();

    [Fact] void should_quote_and_escape_unavailable_check_fields() => _outputs[OutputFormats.Plain].ShouldContain("could_not_check=Observers event_store=\"a namespace=b\" namespace=\"tenant=one\" reason=\"Permission \\\"denied\\\" at C:\\\\store\"");
    [Fact] void should_quote_finding_fields() => _outputs[OutputFormats.Plain].ShouldContain("finding=\"Failed partition\" event_store=\"a namespace=b\" namespace=\"tenant=one\" detail=\"observer\tpartition\"");
    [Fact] void should_quote_scope_fields() => _outputs[OutputFormats.Plain].ShouldContain("scope_event_sequence_tail=10 event_store=\"a namespace=b\" namespace=\"tenant=one\"");
    [Fact] void should_leave_simple_scalar_values_unquoted() => _outputs[OutputFormats.Plain].ShouldContain("server=chronicle://localhost:35000");
}
