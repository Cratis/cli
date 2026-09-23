// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_FileFamilies.when_detecting;

public class and_the_file_has_an_unlisted_extension : Specification
{
    [Fact] void should_fall_back_to_the_documented_default_rules() => FileFamilies.Detect("something.xyz").ShouldEqual(FileFamily.Other);
    [Fact] void should_use_c_style_commented_directives_for_the_default_rules() => FileFamilies.ConfigFor(FileFamily.Other).IfTokens.ShouldContain("//#if");
}
