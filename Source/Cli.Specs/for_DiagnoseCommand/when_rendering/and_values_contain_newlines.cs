// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_values_contain_newlines : given.captured_reports
{
    string[] _lines;

    void Establish() => _data = _data with
    {
        IsAggregate = true,
        ConnectionString = "chronicle://user:secret@localhost:35000/store\r\none",
        ServerVersion = "19.6.1\r\ncurrent",
        LatestServerVersion = "19.6.2\nlatest\ravailable",
        ChecksCouldNotRun = [new DiagnoseCheckFailure("Observers\nquery", "store\r\none", "tenant\none", "Permission\r\ndenied\rhere")],
        Findings = [new DiagnoseFinding("Failed partition\rquery", "store\none", "tenant\rone", "observer\r\npartition\nhere")],
        Scopes = [_data with { EventStore = "store\rone", Namespace = "tenant\r\none" }]
    };

    void Because()
    {
        CaptureReports();
        _lines = _outputs[OutputFormats.Plain].Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact] void should_keep_each_record_on_one_line() => _lines.Length.ShouldEqual(19);
    [Fact] void should_sanitize_the_server() => _lines[3].ShouldEqual("server=\"chronicle://user:***@localhost:35000/store  one\"");
    [Fact] void should_sanitize_the_server_version() => _lines[5].ShouldEqual("server_version=\"19.6.1  current\"");
    [Fact] void should_sanitize_the_latest_server_version() => _lines[6].ShouldEqual("server_version_latest=\"19.6.2 latest available\"");
    [Fact] void should_sanitize_unavailable_check_values() => _lines[16].ShouldEqual("could_not_check=\"Observers query\" event_store=\"store  one\" namespace=\"tenant one\" reason=\"Permission  denied here\"");
    [Fact] void should_sanitize_finding_values() => _lines[17].ShouldEqual("finding=\"Failed partition query\" event_store=\"store one\" namespace=\"tenant one\" detail=\"observer  partition here\"");
    [Fact] void should_sanitize_scope_values() => _lines[18].ShouldEqual("scope_event_sequence_tail=10 event_store=\"store one\" namespace=\"tenant  one\"");
    [Fact] void should_preserve_scalar_key_order() => string.Join(',', _lines.Take(16).Select(x => x.Split('=')[0])).ShouldEqual("healthy,checks_complete,checks_could_not_run,server,reachable,server_version,server_version_latest,event_stores,observers_active,observers_replaying,observers_suspended,observers_disconnected,observers_quarantined,failed_partitions,pending_recommendations,event_sequence_tail");
}
