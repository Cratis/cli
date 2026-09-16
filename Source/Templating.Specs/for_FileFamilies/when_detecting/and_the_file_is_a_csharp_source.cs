// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_FileFamilies.when_detecting;

public class and_the_file_is_a_csharp_source : Specification
{
    [Fact] void should_detect_the_language_family() => FileFamilies.Detect("src/Program.cs").ShouldEqual(FileFamily.Language);
    [Fact] void should_detect_fsharp_as_language() => FileFamilies.Detect("Script.fs").ShouldEqual(FileFamily.Language);
    [Fact] void should_use_raw_directives() => FileFamilies.ConfigFor(FileFamily.Language).IfTokens.ShouldContain("#if");
}
