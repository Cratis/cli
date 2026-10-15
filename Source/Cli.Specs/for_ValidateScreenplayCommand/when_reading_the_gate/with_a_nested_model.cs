// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_a_nested_model : given.a_validation_gate_cli_process
{
    string _child;
    void Establish()
    {
        Declare("{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true}}");
        _child = Directory.CreateDirectory(Path.Combine(_root, "child")).FullName;
        File.WriteAllText(Path.Combine(_child, "screenplay.json"), "{\"validation\":{\"checks\":[],\"warningsAsErrors\":false}}");
        File.Move(_document, Path.Combine(_child, "child.play"));
        _document = Path.Combine(_child, "child.play");
    }
    Task Because() => Run();
    [Fact] void should_use_the_nested_gate() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_the_nested_root() => Gate.GetProperty("modelRoot").GetString().ShouldEqual(_child);
    [Fact] void should_not_inherit_parent_checks() => Gate.GetProperty("checks").GetProperty("value").GetArrayLength().ShouldEqual(0);
}
