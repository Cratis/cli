// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFileTargetResolver.when_resolving;

public class and_the_path_is_a_play_file : given.a_temporary_folder
{
    string _document;
    ScreenplayTarget _result;

    void Establish()
    {
        _document = Path.Combine(_folder, "MyApp.play");
        File.WriteAllText(_document, "domain Library\n");
    }

    void Because() => _result = PlayFileTargetResolver.Resolve("MyApp.play", _folder);

    [Fact] void should_resolve() => _result.IsResolved.ShouldBeTrue();
    [Fact] void should_resolve_the_document_relative_to_the_current_directory() => _result.Path.ShouldEqual(_document);
    [Fact] void should_not_report_an_error() => _result.Error.ShouldBeNull();
}
