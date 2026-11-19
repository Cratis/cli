// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_RunProfileCheck;

public class when_scene_conversion_refuses_ui_syntax : Specification
{
    string _folder = null!;
    string _file = null!;
    UnsupportedUiSyntax _failure = null!;
    IReadOnlyList<ScreenplayDiagnostic> _findings = null!;
    ApplicationSyntax? _application;
    SceneApplication? _scene;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("cratis-run-unsupported-ui-").FullName;
        _file = Path.Combine(_folder, "application.play");
        File.WriteAllText(_file, "ui profile Desktop\n  target platform web\n");
        _failure = new("FutureUiSyntax", new SourceLocation(7, 9, "profile.play"));
    }

    void Because() => (_findings, _application, _scene) = RunProfileCheck.CompileAndCheck(
        new FileInfo(_file), ApprovedPackageSet.Empty, false, _ => throw _failure);

    [Fact] void should_report_the_conversion_code() => _findings.Single().Code.ShouldEqual(UnsupportedUiSyntax.DiagnosticCode);
    [Fact] void should_report_an_error() => _findings.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_preserve_the_conversion_message() => _findings.Single().Message.ShouldEqual(_failure.Message);
    [Fact] void should_preserve_the_authored_location() => _findings.Single().Location.ShouldEqual(_failure.Location.ToString());
    [Fact] void should_not_return_an_application_to_run() => _application.ShouldBeNull();
    [Fact] void should_not_return_a_scene_to_serve() => _scene.ShouldBeNull();

    void Destroy() => Directory.Delete(_folder, true);
}
