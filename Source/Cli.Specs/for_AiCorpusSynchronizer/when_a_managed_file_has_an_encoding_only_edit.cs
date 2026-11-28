// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_AiCorpusSynchronizer;

public class when_a_managed_file_has_an_encoding_only_edit : given.a_corpus_with_profile_scoped_rules
{
    static readonly AiConfiguration _configuration = new([], ["cratis/example"], []);
    byte[] _editedBytes = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        var path = Installed("general.md");
        File.WriteAllText(path, File.ReadAllText(path), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        _editedBytes = File.ReadAllBytes(path);
    }

    [Fact] void should_report_the_file_as_modified_in_status() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldContain("rules/general.md");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_the_edited_bytes_and_report_a_conflict(bool uninstall)
    {
        var result = uninstall ? AiCorpusSynchronizer.Uninstall(_project) : AiCorpusSynchronizer.Synchronize(_project, _corpus, _configuration);
        result.Conflicts.ShouldContain("rules/general.md");
        File.ReadAllBytes(Installed("general.md")).ShouldEqual(_editedBytes);
    }
}
