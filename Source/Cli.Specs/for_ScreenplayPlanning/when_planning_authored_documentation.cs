// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_authored_documentation : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _content = null!;

    void Establish() => File.WriteAllText(_file, Source
        .Replace("      command RegisterProject\n", "      command RegisterProject\n        description \"Registers <project> & name\"\n", StringComparison.Ordinal)
        .Replace("      event ProjectRegistered\n", "      event ProjectRegistered\n        description \"A project was registered\"\n        documentation\n          ```markdown\n          Keep the project identity.\n          ```\n", StringComparison.Ordinal));

    async Task Because()
    {
        _result = await Plan();
        _content = Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs").Bytes.AsSpan());
    }

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_render_the_command_description_as_an_escaped_summary() => _content.ShouldContain("/// <summary>\n/// Registers &lt;project&gt; &amp; name\n/// </summary>");
    [Fact] void should_render_the_event_description_as_a_summary() => _content.ShouldContain("/// <summary>\n/// A project was registered\n/// </summary>");
    [Fact] void should_render_the_event_documentation_as_remarks() => _content.ShouldContain("/// <remarks>\n/// Keep the project identity.\n/// </remarks>");

    [Fact]
    public async Task should_change_artifact_hashes_without_changing_the_executable_revision()
    {
        await File.WriteAllTextAsync(_file, Source.Replace("      command RegisterProject\n", "      command RegisterProject\n        description \"Another explanation\"\n", StringComparison.Ordinal));

        var changed = await Plan();

        changed.Success.ShouldBeTrue();
        changed.Artifacts!.SemanticRevision.ShouldEqual(_result.Artifacts!.SemanticRevision);
        changed.Artifacts.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs").Sha256
            .ShouldNotEqual(_result.Artifacts.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs").Sha256);
    }
}
