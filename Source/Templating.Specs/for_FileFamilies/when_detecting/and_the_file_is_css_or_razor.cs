// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_FileFamilies.when_detecting;

public class and_the_file_is_css_or_razor : Specification
{
    [Fact] void should_use_block_comment_directives_for_css() => FileFamilies.ConfigFor(FileFamilies.Detect("site.css")).IfTokens.ShouldContain("/*#if");
    [Fact] void should_use_razor_comment_directives_for_razor() => FileFamilies.ConfigFor(FileFamilies.Detect("Index.cshtml")).IfTokens.ShouldContain("@*#if");
    [Fact] void should_use_rem_directives_for_command_files() => FileFamilies.ConfigFor(FileFamilies.Detect("build.cmd")).IfTokens.ShouldContain("rem #if");
}
