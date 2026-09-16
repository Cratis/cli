// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_FileFamilies.when_detecting;

public class and_the_file_is_json : Specification
{
    [Fact] void should_detect_the_json_family() => FileFamilies.Detect("appsettings.json").ShouldEqual(FileFamily.Json);
    [Fact] void should_use_c_style_commented_directives() => FileFamilies.ConfigFor(FileFamily.Json).IfTokens.ShouldContain("//#if");
    [Fact] void should_have_actionable_variants() => FileFamilies.ConfigFor(FileFamily.Json).ActionableIfTokens.ShouldContain("////#if");
}
