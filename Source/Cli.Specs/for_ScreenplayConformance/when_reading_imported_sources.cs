// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_reading_imported_sources : Specification
{
    string _folder = null!;
    AuthoredScreenplay _model = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        File.WriteAllText(Path.Combine(_folder, "Root.play"), "import \"Events.play\"\ndomain Library\n");
        File.WriteAllText(Path.Combine(_folder, "Events.play"), "module Library\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n");
    }
    void Because() => _model = new ScreenplayConformance().Read(Path.Combine(_folder, "Root.play"));
    [Fact] void should_recover_the_authored_name() => _model.ApplicationName.ShouldEqual("Library");
    [Fact] void should_include_every_imported_document() => _model.Sources.Keys.Order(StringComparer.Ordinal).ShouldEqual("Events.play", "Root.play");
    [Fact] void should_not_include_unrelated_files() => _model.Sources.Count.ShouldEqual(2);
    [Fact] void should_have_no_source_errors() => _model.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    void Destroy() => Directory.Delete(_folder, true);
}
