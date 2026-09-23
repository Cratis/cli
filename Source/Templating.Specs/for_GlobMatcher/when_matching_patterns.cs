// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.FileSystem;

namespace Cratis.Templating.Specs.for_GlobMatcher;

public class when_matching_patterns : Specification
{
    [Fact] void should_match_double_star_prefix_at_any_depth() => GlobMatcher.Matches("a/b/c/file.cs", "**/*.cs").ShouldBeTrue();
    [Fact] void should_match_double_star_prefix_at_the_root() => GlobMatcher.Matches("file.cs", "**/*.cs").ShouldBeTrue();
    [Fact] void should_match_single_star_within_a_segment() => GlobMatcher.Matches("folder/file.cs", "folder/*.cs").ShouldBeTrue();
    [Fact] void should_not_cross_directories_with_single_star() => GlobMatcher.Matches("a/b/file.cs", "*.cs").ShouldBeFalse();
    [Fact] void should_match_bare_pattern_only_at_root_level() => GlobMatcher.Matches("file.cs", "file.cs").ShouldBeTrue();
    [Fact] void should_match_question_mark_for_one_character() => GlobMatcher.Matches("file1.cs", "file?.cs").ShouldBeTrue();
    [Fact] void should_match_directory_globs_for_everything_below() => GlobMatcher.Matches("bin/obj/x.txt", "**/bin/**").ShouldBeTrue();
    [Fact] void should_match_template_config_pattern() => GlobMatcher.Matches("nested/.template.config/template.json", "**/.template.config/**").ShouldBeTrue();
    [Fact] void should_not_match_non_matching_patterns() => GlobMatcher.Matches("folder/file.cs", "**/*.md").ShouldBeFalse();
}
