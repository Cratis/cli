// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_a_subfolder_target : given.a_validation_gate_cli_process
{
    void Establish()
    {
        Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true}}");
        var child = Directory.CreateDirectory(Path.Combine(_root, "child")).FullName;
        File.Move(_document, Path.Combine(child, "child.play"));
        _document = child;
    }
    Task Because() => Run();
    [Fact] void should_apply_the_owning_gate() => _exitCode.ShouldEqual(5);
    [Fact] void should_report_the_owning_root() => Gate.GetProperty("modelRoot").GetString().ShouldEqual(_root);
    [Fact] void should_compile_only_the_selected_subfolder() => Summary.GetProperty("files").GetInt32().ShouldEqual(1);
}
