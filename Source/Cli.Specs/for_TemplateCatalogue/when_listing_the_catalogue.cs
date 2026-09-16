// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateCatalogue;

public class when_listing_the_catalogue : Specification
{
    [Fact] void should_list_the_four_cratis_templates() => TemplateCatalogue.List().Count.ShouldEqual(4);

    [Fact] void should_carry_the_expected_short_names() =>
        TemplateCatalogue.List().Select(template => template.ShortName).ShouldContainOnly(
        [
            "cratis", "cratis-aspire", "cratis-chronicle-console", "cratis-chronicle-web"
        ]);
}
