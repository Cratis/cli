// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnosticsWriter.when_writing_text;

[Collection(CliSpecsCollection.Name)]
public class and_source_policy_is_available : Specification
{
    TextWriter _previousError;
    StringWriter _error;
    string _result;

    void Establish()
    {
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    void Because()
    {
        ScreenplayDiagnosticsWriter.Write(
            OutputFormats.Plain,
            [],
            new ScreenplayGenerationProvenance(
                ScreenplayProviders.CritterStack,
                "0.19.0",
                [
                    new ScreenplayProjectProvenance("Application", "net10.0", [], [], [])
                    {
                        SourcePolicy = new ScreenplaySourcePolicyProvenance(
                            "Source/Application/Application.csproj",
                            "Source/Application/Application",
                            1,
                            "Workspace",
                            "Ordinal"),
                        SourceStructure = new ScreenplaySourceStructureProvenance(
                            "Application",
                            1,
                            "Features",
                            "Lending",
                            2)
                    }
                ],
                null));
        _result = _error.ToString();
    }

    [Fact] void should_report_the_logical_project() => _result.ShouldContain("logical project: Source/Application/Application.csproj");
    [Fact] void should_report_the_stable_identity() => _result.ShouldContain("project identity: Source/Application/Application");
    [Fact] void should_report_the_policy() => _result.ShouldContain("source policy: version 1, Workspace display root, Ordinal case policy");
    [Fact] void should_report_the_project_role() => _result.ShouldContain("project role: Application");
    [Fact] void should_report_the_source_structure_policy() => _result.ShouldContain("source structure: version 1, feature root Features, module Lending, 2 namespace segments skipped");
    [Fact] void should_not_report_a_physical_root() => _result.ShouldNotContain("/physical/");

    void Destroy()
    {
        Console.SetError(_previousError);
        _error.Dispose();
    }
}
