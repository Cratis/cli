// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;

namespace Cratis.Cli.for_RunProfileCheck;

/// <summary>
/// A <c language="screenplay">root template</c> that cannot be the application's shell stops <c language="shell">cratis run</c> before Docker starts: the compiler's
/// <c language="shell">PLAY0790</c>-<c language="shell">PLAY0799</c> errors are reported with their code and location, and a version pin no installed blueprint satisfies is
/// reported with <c language="shell">CLI-RENDER-019</c> (Cratis/Stage#466, Cratis/cli#377).
/// </summary>
public class when_checking_a_root_template : Specification
{
    const string Shell = "layout AppFrame\n  topbar\n  content\n\n  arrangement flow\n    column\n      topbar height 56\n      content grow\n\n";

    string _root;
    IReadOnlyList<ScreenplayDiagnostic> _own;
    IReadOnlyList<ScreenplayDiagnostic> _blueprint;
    IReadOnlyList<ScreenplayDiagnostic> _pin;
    IReadOnlyList<ScreenplayDiagnostic> _unknown;
    IReadOnlyList<ScreenplayDiagnostic> _options;

    void Establish() => _root = Directory.CreateTempSubdirectory("cratis-run-root-").FullName;

    void Because()
    {
        _own = Check("own", "  root template AppFrame\n    option showFooter false\n  appearance\n    color scheme dark\n    accent \"#4f46e5\"\n  packages\n    core\n");
        _blueprint = Check("blueprint", "  root template blueprint Cratis.Blueprint.Default AppShell version \"^1.0\"\n    option navigationMode slim\n  packages\n    core\n    Cratis.Blueprint.Default\n");
        _pin = Check("pin", "  root template blueprint Cratis.Blueprint.Default AppShell version \"^99.0\"\n  packages\n    core\n    Cratis.Blueprint.Default\n");
        _unknown = Check("unknown", "  root template Nowhere\n  packages\n    core\n");
        _options = Check("options", "  root template AppFrame\n  appearance\n    color scheme sepia\n  packages\n    core\n");
    }

    void Destroy() => Directory.Delete(_root, true);

    IReadOnlyList<ScreenplayDiagnostic> Check(string name, string profile)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root, name)).FullName;
        File.WriteAllText(Path.Combine(folder, "application.play"), Shell + "ui profile Desktop\n  target platform web\n  target size expanded\n" + profile);
        return RunProfileCheck.Check(new DirectoryInfo(folder), ApprovedPackageSet.Empty);
    }

    [Fact] void should_start_with_an_own_root_template_its_options_and_appearance() => _own.ShouldBeEmpty();
    [Fact] void should_start_with_a_blueprint_root_template_and_its_pin() => _blueprint.ShouldBeEmpty();
    [Fact] void should_refuse_a_pin_no_installed_blueprint_satisfies() => _pin.Single().Code.ShouldEqual("CLI-RENDER-019");
    [Fact] void should_name_the_template_and_the_pin() => (_pin.Single().Message.Contains("AppShell", StringComparison.Ordinal) && _pin.Single().Message.Contains("99", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_root_template_with_its_compiler_code() => _unknown.ShouldContain(_ => _.Code == "PLAY0790" && _.Location!.Contains("application.play(", StringComparison.Ordinal));
    [Fact] void should_refuse_an_invalid_appearance_with_its_compiler_code() => _options.ShouldContain(_ => _.Code == "PLAY0797");
    [Fact] void should_report_only_errors_before_starting() => _unknown.Concat(_options).Concat(_pin).All(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeTrue();
}
