// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration.when_building_the_targets_file;

public class and_the_chained_path_holds_markup_characters : given.a_temporary_folder
{
    string _chained;
    string _result;

    void Establish()
    {
        _chained = Path.Combine(_folder, "Custom & After.targets");
        File.WriteAllText(_chained, "<Project />");
    }

    void Because() => _result = DesignTimeResourceGeneration.Targets(_chained);

    [Fact] void should_escape_them() => _result.ShouldContain("Custom &amp; After.targets");
    [Fact] void should_leave_the_file_readable_as_markup() => _result.ShouldNotContain("& After");
}
