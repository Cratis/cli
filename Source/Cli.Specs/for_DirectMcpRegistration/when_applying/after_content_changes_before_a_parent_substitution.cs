// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_content_changes_before_a_parent_substitution : given.a_home_and_a_project
{
    const string Planned = "{\"theme\":\"dark\"}\n";
    const string Edited = "{\"theme\":\"concurrently edited\"}\n";
    Exception? _error;
    bool _written;

    void Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        Write(ProjectFile(".cursor/mcp.json"), Edited);
    }

    void Because()
    {
        if (OperatingSystem.IsWindows()) return;
        var path = Path.Combine(AiPhysicalRoot.Resolve(_project), ".cursor/mcp.json");
        void SubstituteParent()
        {
            var parent = Path.GetDirectoryName(path)!;
            Directory.Move(parent, parent + ".edited");
            Write(path, Planned);
            File.ReadAllText(path).ShouldEqual(Planned);
        }
        _error = Catch.Exception(() => AiConfigurationFile.Write(path, "{}", Encoding.UTF8.GetBytes(Planned), SubstituteParent, beforeOpen: null, write: null, () => _written = true));
    }

    [given.unix_only.Fact] void should_refuse_the_handle_that_differs_from_the_plan() => _error.ShouldBeOfExactType<IOException>();
    [given.unix_only.Fact] void should_leave_the_concurrent_edit_untouched() => File.ReadAllText(ProjectFile(".cursor.edited/mcp.json")).ShouldEqual(Edited);
    [given.unix_only.Fact] void should_leave_the_pathname_replacement_untouched() => File.ReadAllText(ProjectFile(".cursor/mcp.json")).ShouldEqual(Planned);
    [given.unix_only.Fact] void should_not_start_writing() => _written.ShouldBeFalse();
}
