// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_script_execute_bits_are_missing : given.a_corpus_with_script_permissions
{
    const UnixFileMode Restricted = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead;
    Dictionary<string, string> _before = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        _before = new[] { "executable.sh", "shebang.mjs", "library.mjs" }.ToDictionary(name => name, name => File.ReadAllText(Installed(name)));
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(Installed("executable.sh"), ReadWrite);
        File.SetUnixFileMode(Installed("shebang.mjs"), Restricted);
        File.SetUnixFileMode(Installed("library.mjs"), Restricted);
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);

    [Fact] void should_update_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_all_content_unchanged() => _before.ToDictionary(pair => pair.Key, pair => File.ReadAllText(Installed(pair.Key))).ShouldEqual(_before);
    [Fact] void should_repair_0644_to_0755()
    {
        if (!OperatingSystem.IsWindows()) File.GetUnixFileMode(Installed("executable.sh")).ShouldEqual(ReadWrite | ExecuteBits);
    }
    [Fact] void should_add_execute_only_for_readable_permission_classes()
    {
        if (!OperatingSystem.IsWindows()) File.GetUnixFileMode(Installed("shebang.mjs")).ShouldEqual(Restricted | UnixFileMode.UserExecute | UnixFileMode.GroupExecute);
    }
    [Fact] void should_preserve_non_script_permissions()
    {
        if (!OperatingSystem.IsWindows()) File.GetUnixFileMode(Installed("library.mjs")).ShouldEqual(Restricted);
    }
    [Fact] void should_report_each_updated_file() => _result.Actions.ShouldContainOnly("Updated hooks/scripts/executable.sh", "Updated hooks/scripts/shebang.mjs", "Updated hooks/scripts/library.mjs");
}
