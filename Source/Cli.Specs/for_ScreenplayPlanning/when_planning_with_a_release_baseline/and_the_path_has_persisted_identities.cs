// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_the_path_has_persisted_identities : for_ScreenplayReleaseBaseline.given.a_release_model
{
    ScreenplayRenderPlan _result = null!;
    string[] _metadataBefore = null!;
    string[] _metadataAfter = null!;

    protected override string ModelSource => "concept RecordId : Uuid\nmodule M\n  feature F\n    slice StateChange Record\n      command Record\n        recordId RecordId identifier\n        value String\n        produces Recorded\n          for recordId\n          value = value\n      event Recorded\n        value String\n";

    void Establish()
    {
        ReleaseBaselineCommands.Admit(_root, "v1.0").ExitCode.ShouldEqual(0);
        File.WriteAllText(_document, ModelSource.Replace("value String", "value Int", StringComparison.Ordinal));
        _metadataBefore = Metadata();
    }

    async Task Because()
    {
        _result = await new ScreenplayPlanning().Plan(new(_root, "Model", "cratis"), CancellationToken.None);
        _metadataAfter = Metadata();
    }

    string[] Metadata() => [.. Directory.EnumerateFiles(Path.Combine(_root, ".screenplay"), "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(path => $"{Path.GetRelativePath(_root, path)}:{Convert.ToHexString(File.ReadAllBytes(path))}")];

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_real_persisted_identity_violation() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-001");
    [Fact] void should_not_write_metadata() => _metadataAfter.ShouldEqual(_metadataBefore);
}
