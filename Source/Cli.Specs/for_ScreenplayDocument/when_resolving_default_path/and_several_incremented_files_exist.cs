// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDocument.when_resolving_default_path;

public class and_several_incremented_files_exist : given.a_temporary_folder
{
    string _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_folder, "Screenplay.play"), string.Empty);
        File.WriteAllText(Path.Combine(_folder, "Screenplay-1.play"), string.Empty);
        File.WriteAllText(Path.Combine(_folder, "Screenplay-2.play"), string.Empty);
    }

    void Because() => _result = ScreenplayDocument.ResolveDefaultPath(_folder);

    [Fact] void should_resolve_to_the_first_free_name() => _result.ShouldEqual(Path.Combine(_folder, "Screenplay-3.play"));
}
