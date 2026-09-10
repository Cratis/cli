// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_mapping;

public class with_a_path_bearing_workspace_failure : Specification
{
    const string PhysicalRoot = "/private/checkout";

    ScreenplayDiagnostic _diagnostic;
    string _json;

    void Because()
    {
        _diagnostic = ScreenplayCompilationLoader.WorkspaceFailure(
            ScreenplayDiagnosticLocations.Target($"{PhysicalRoot}/Application.slnx"));
        _json = ScreenplayDiagnosticsWriter.JsonFor(OutputFormats.JsonCompact, [_diagnostic], null);
    }

    [Fact] void should_report_the_stable_workspace_failure_code() => _diagnostic.Code.ShouldEqual(ScreenplayDiagnosticCodes.WorkspaceFailure);
    [Fact] void should_report_a_stable_non_disclosing_message() => _diagnostic.Message.ShouldEqual("MSBuild reported a workspace problem while loading the target");
    [Fact] void should_use_the_logical_target_location() => _diagnostic.Location.ShouldEqual("Application.slnx");
    [Fact] void should_not_serialize_the_physical_root() => _json.ShouldNotContain(PhysicalRoot);
    [Fact] void should_produce_valid_json() => JsonDocument.Parse(_json).RootElement.GetProperty("diagnostics").GetArrayLength().ShouldEqual(1);
}
