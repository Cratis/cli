// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_declared_scope : given.a_validation_gate_cli_process
{
    void Establish()
    {
        Declare("{\"validation\":{\"scope\":\"M.F.Clean\",\"warningsAsErrors\":true}}");
        File.WriteAllText(_document, "module M\n  feature F\n    slice StateView Clean\n      event Changed\n        id Uuid\n    slice StateView Dirty\n      event Broken\n        broken\n");
    }
    Task Because() => Run();
    [Fact] void should_pass_the_clean_scope() => _exitCode.ShouldEqual(0);
    [Fact] void should_select_the_declared_scope() => Summary.GetProperty("scope").GetString().ShouldEqual("M.F.Clean");
    [Fact] void should_attribute_scope_to_the_file() => SourceOf("scope").ShouldEqual("file");
    [Fact] void should_keep_whole_application_invalid() => Summary.GetProperty("wholeApplication").GetProperty("valid").GetBoolean().ShouldBeFalse();
}
