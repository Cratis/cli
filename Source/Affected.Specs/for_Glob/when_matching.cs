// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_Glob;

public class when_matching : Specification
{
    [Fact] void should_match_a_bare_name_at_any_depth() => Glob.IsMatch("Directory.Build.props", "a/b/Directory.Build.props").ShouldBeTrue();
    [Fact] void should_match_a_bare_name_at_the_root() => Glob.IsMatch("global.json", "global.json").ShouldBeTrue();
    [Fact] void should_not_match_a_longer_name() => Glob.IsMatch("global.json", "myglobal.json").ShouldBeFalse();
    [Fact] void should_keep_a_single_star_within_a_segment() => Glob.IsMatch("docs/*.md", "docs/a/b.md").ShouldBeFalse();
    [Fact] void should_cross_segments_with_a_double_star() => Glob.IsMatch("docs/**", "docs/a/b.md").ShouldBeTrue();
    [Fact] void should_match_nothing_deeper_when_anchored() => Glob.IsMatch("scripts/**", "other/scripts/a.sh").ShouldBeFalse();
    [Fact] void should_ignore_case_when_asked() => Glob.IsMatch("NuGet.config", "nuget.config", ignoreCase: true).ShouldBeTrue();
    [Fact] void should_be_case_sensitive_by_default() => Glob.IsMatch("NuGet.config", "nuget.config").ShouldBeFalse();
}
